using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Entities;
using IslamicApp.Application.DTOs;

namespace IslamicApp.Infrastructure.Search;

public class NormalizeStage : ISearchPipelineStage
{
    private readonly ISearchNormalizer _normalizer;

    public NormalizeStage(ISearchNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public Task ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        context.NormalizedQuery = _normalizer.Normalize(context.Query.OriginalQuery);
        sw.Stop();
        
        context.Diagnostics = context.Diagnostics with 
        { 
            NormalizationTimeMs = sw.Elapsed.TotalMilliseconds 
        };
        
        return Task.CompletedTask;
    }
}

public class TokenizeStage : ISearchPipelineStage
{
    private readonly ITokenizer _tokenizer;

    public TokenizeStage(ITokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public Task ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        context.RawTokens = _tokenizer.Tokenize(context.Query.OriginalQuery);
        context.NormalizedTokens = _tokenizer.Tokenize(context.NormalizedQuery);
        
        // Remove stop words from unique tokens
        string detectedLanguage = DetermineLanguage(context.NormalizedQuery);
        context.UniqueTokens = _tokenizer.RemoveStopwords(context.NormalizedTokens, detectedLanguage)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.CompletedTask;
    }

    private static string DetermineLanguage(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return "en";
        // Simple script detection: if it contains Arabic characters, return "ar"
        if (query.Any(c => c >= 0x0600 && c <= 0x06FF)) return "ar";
        return "en";
    }
}

public class SynonymExpansionStage : ISearchPipelineStage
{
    private readonly ISynonymEngine _synonymEngine;

    public SynonymExpansionStage(ISynonymEngine synonymEngine)
    {
        _synonymEngine = synonymEngine;
    }

    public Task ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        context.ExpandedTokens = _synonymEngine.ExpandTokens(context.UniqueTokens, out var weights);
        return Task.CompletedTask;
    }
}

public class ReferenceResolutionStage : ISearchPipelineStage
{
    private readonly ISourceReferenceResolver _resolver;

    public ReferenceResolutionStage(ISourceReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public Task ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        if (_resolver.TryResolve(context.Query.OriginalQuery, out var reference))
        {
            context.ResolvedReference = reference;
        }
        return Task.CompletedTask;
    }
}

public class DatabaseQueryStage : ISearchPipelineStage
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseQueryStage(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var candidates = new List<SearchCandidate>();

        // Scenario 1: Reference match
        if (context.ResolvedReference != null)
        {
            var match = Regex.Match(context.ResolvedReference.Reference, @"^(\d+):(\d+)(?:-(\d+))?$");
            if (match.Success)
            {
                int surah = int.Parse(match.Groups[1].Value);
                int startAyah = int.Parse(match.Groups[2].Value);
                int endAyah = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : startAyah;

                var verses = await _dbContext.QuranVerses
                    .AsNoTracking()
                    .Include(v => v.Surah)
                    .Include(v => v.Translations)
                    .Where(v => v.SurahNumber == surah && v.AyahNumber >= startAyah && v.AyahNumber <= endAyah)
                    .ToListAsync(cancellationToken);

                foreach (var v in verses)
                {
                    candidates.Add(MapToCandidate(v));
                }
            }
        }
        else
        {
            // Scenario 2: Keyword search across expanded tokens
            var targetTokens = context.ExpandedTokens;
            if (targetTokens.Count > 0)
            {
                var matchedIds = new HashSet<string>();

                foreach (var token in targetTokens)
                {
                    // Trigram matching translations
                    var transIds = await _dbContext.QuranTranslations
                        .Where(t => t.Text.Contains(token))
                        .Select(t => t.VerseId)
                        .Take(50)
                        .ToListAsync(cancellationToken);
                    
                    foreach (var id in transIds) matchedIds.Add(id);

                    // Trigram matching verses (ArabicCleaned)
                    var verseIds = await _dbContext.QuranVerses
                        .Where(v => v.ArabicCleaned.Contains(token))
                        .Select(v => v.Id)
                        .Take(50)
                        .ToListAsync(cancellationToken);

                    foreach (var id in verseIds) matchedIds.Add(id);

                    // Trigram matching Surah names
                    var surahVerseIds = await _dbContext.QuranVerses
                        .Where(v => v.Surah.EnglishName.Contains(token) || v.Surah.Transliteration.Contains(token))
                        .Select(v => v.Id)
                        .Take(30)
                        .ToListAsync(cancellationToken);

                    foreach (var id in surahVerseIds) matchedIds.Add(id);
                }

                // Batch fetch matched verses
                var matchedVerses = await _dbContext.QuranVerses
                    .AsNoTracking()
                    .Include(v => v.Surah)
                    .Include(v => v.Translations)
                    .Where(v => matchedIds.Contains(v.Id))
                    .ToListAsync(cancellationToken);

                foreach (var v in matchedVerses)
                {
                    candidates.Add(MapToCandidate(v));
                }
            }
        }

        context.Candidates = candidates;
        sw.Stop();
        context.Diagnostics = context.Diagnostics with 
        { 
            QueryTimeMs = sw.Elapsed.TotalMilliseconds,
            TotalMatches = candidates.Count
        };
    }

    private static SearchCandidate MapToCandidate(QuranVerseEntity v)
    {
        var meta = new Dictionary<string, object>
        {
            { "SurahNumber", v.SurahNumber },
            { "AyahNumber", v.AyahNumber },
            { "SurahEnglishName", v.Surah.EnglishName }
        };

        return new SearchCandidate(
            SourceType: "Quran",
            SourceName: "Qur'an",
            Reference: $"{v.SurahNumber}:{v.AyahNumber}",
            PrimaryText: v.ArabicCleaned,
            OriginalLanguage: "ar",
            Translations: v.Translations
                .Select(t => new TranslationDto { Language = t.Language, Translator = t.Translator, Text = t.Text })
                .ToList(),
            Metadata: meta
        );
    }
}

public class RankingStage : ISearchPipelineStage
{
    private readonly IRankingEngine _rankingEngine;

    public RankingStage(IRankingEngine rankingEngine)
    {
        _rankingEngine = rankingEngine;
    }

    public Task ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _rankingEngine.Rank(context);
        sw.Stop();
        
        context.Diagnostics = context.Diagnostics with 
        { 
            RankingTimeMs = sw.Elapsed.TotalMilliseconds 
        };
        
        return Task.CompletedTask;
    }
}

public class HighlightStage : ISearchPipelineStage
{
    // Highlights are built during EvidenceBuildStage dynamically
    public Task ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class EvidenceBuildStage : ISearchPipelineStage
{
    private readonly IEvidenceBuilder _evidenceBuilder;

    public EvidenceBuildStage(IEvidenceBuilder evidenceBuilder)
    {
        _evidenceBuilder = evidenceBuilder;
    }

    public Task ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        
        // Paginate ranked results
        var page = context.Options.Page;
        var pageSize = context.Options.PageSize;

        var paginatedCandidates = context.RankedCandidates
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var evidenceItems = new List<EvidenceItem>();
        foreach (var candidate in paginatedCandidates)
        {
            evidenceItems.Add(_evidenceBuilder.BuildItem(candidate));
        }

        context.EvidenceItems = evidenceItems;
        sw.Stop();

        context.Diagnostics = context.Diagnostics with 
        { 
            EvidenceBuildTimeMs = sw.Elapsed.TotalMilliseconds,
            ReturnedMatches = evidenceItems.Count
        };

        return Task.CompletedTask;
    }
}
