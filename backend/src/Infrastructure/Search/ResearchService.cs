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
using IslamicApp.Application.DTOs;

namespace IslamicApp.Infrastructure.Search;

public class ResearchService : IResearchService
{
    private readonly ISearchPipeline _pipeline;
    private readonly ISearchNormalizer _normalizer;
    private readonly ITokenizer _tokenizer;
    private readonly ISynonymEngine _synonymEngine;
    private readonly ISourceReferenceResolver _resolver;
    private readonly IRankingConfiguration _rankingConfig;
    private readonly IEvidenceBuilder _evidenceBuilder;
    private readonly ApplicationDbContext _dbContext;
    private readonly SuggestionIndex _suggestionIndex;

    public ResearchService(
        ISearchPipeline pipeline,
        ISearchNormalizer normalizer,
        ITokenizer tokenizer,
        ISynonymEngine synonymEngine,
        ISourceReferenceResolver resolver,
        IRankingConfiguration rankingConfig,
        IEvidenceBuilder evidenceBuilder,
        ApplicationDbContext dbContext,
        SuggestionIndex suggestionIndex)
    {
        _pipeline = pipeline;
        _normalizer = normalizer;
        _tokenizer = tokenizer;
        _synonymEngine = synonymEngine;
        _resolver = resolver;
        _rankingConfig = rankingConfig;
        _evidenceBuilder = evidenceBuilder;
        _dbContext = dbContext;
        _suggestionIndex = suggestionIndex;
    }

    public async Task<EvidenceDossier> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var totalSw = Stopwatch.StartNew();
        var context = new SearchContext(query, query.Options);

        // Execute Search Pipeline
        await _pipeline.ExecuteAsync(context, cancellationToken);
        totalSw.Stop();

        // Trace execution statistics and checksums
        string tokenizerChk = (_tokenizer as Tokenizer)?.Checksum ?? "n/a";
        string synonymChk = (_synonymEngine as SynonymEngine)?.Checksum ?? "n/a";
        string aliasChk = (_resolver as SourceReferenceResolver)?.AliasChecksum ?? "n/a";
        string rankingChk = (_rankingConfig as RankingConfiguration)?.Checksum ?? "n/a";

        var searchId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;

        var execContext = new SearchExecutionContext(
            SearchId: searchId,
            StartedAt: startedAt,
            OriginalQuery: query.OriginalQuery,
            NormalizedQuery: context.NormalizedQuery,
            Language: context.UniqueTokens.Count > 0 ? "en" : "ar",
            Strategy: context.ResolvedReference != null ? "ReferenceMatch" : "LexicalSearch",
            RankingChecksum: rankingChk,
            SynonymChecksum: synonymChk,
            AliasChecksum: aliasChk,
            StopwordChecksum: tokenizerChk
        );

        context.ExecutionContext = execContext;

        var diagnostics = context.Diagnostics with 
        { 
            ExecutionTimeMs = totalSw.Elapsed.TotalMilliseconds 
        };

        // Determine strategy description
        string strategy = context.ResolvedReference != null ? "Reference" : "Keyword";

        // Segment candidates into Primary (Score >= 50) and Supporting (Score < 50)
        var primary = context.EvidenceItems.Where(e => e.Score >= 50).ToList();
        var supporting = context.EvidenceItems.Where(e => e.Score < 50).ToList();

        // Fallback: if it's a reference search or no query tokens match, treat all items as primary
        if (context.ResolvedReference != null)
        {
            primary = context.EvidenceItems;
            supporting = new List<EvidenceItem>();
        }

        var export = new ExportMetadata(
            GeneratedAt: startedAt,
            SearchId: searchId,
            ApplicationVersion: "1.0",
            DatasetVersions: "Quran-v1",
            ExecutionTimeMs: totalSw.Elapsed.TotalMilliseconds,
            SourcesUsed: new List<string> { "Quran" },
            Language: execContext.Language
        );

        // Save diagnostics to context before building dossier
        context.Diagnostics = diagnostics;

        // Build related topics and references statically or dynamically
        var relatedRefs = context.EvidenceItems
            .Select(e => e.Reference)
            .Take(5)
            .ToList();

        var relatedTopics = context.UniqueTokens
            .Concat(context.ExpandedTokens)
            .Distinct()
            .Take(5)
            .ToList();

        return new EvidenceDossier(
            ExecutionContext: execContext,
            Summary: $"Search strategy: {strategy}. Total matches: {diagnostics.TotalMatches}.",
            PrimaryEvidence: primary,
            SupportingEvidence: supporting,
            RelatedReferences: relatedRefs,
            RelatedTopics: relatedTopics,
            ExportMetadata: export
        );
    }

    public async Task<EvidenceItem?> GetReferenceAsync(string reference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        if (_resolver.TryResolve(reference, out var resolved) && resolved != null)
        {
            var match = Regex.Match(resolved.Reference, @"^(\d+):(\d+)$");
            if (match.Success)
            {
                int surah = int.Parse(match.Groups[1].Value);
                int ayah = int.Parse(match.Groups[2].Value);

                var v = await _dbContext.QuranVerses
                    .AsNoTracking()
                    .Include(v => v.Surah)
                    .Include(v => v.Translations)
                    .FirstOrDefaultAsync(v => v.SurahNumber == surah && v.AyahNumber == ayah, cancellationToken);

                if (v != null)
                {
                    var meta = new Dictionary<string, object>
                    {
                        { "SurahNumber", v.SurahNumber },
                        { "AyahNumber", v.AyahNumber },
                        { "SurahEnglishName", v.Surah.EnglishName }
                    };

                    var candidate = new SearchCandidate(
                        SourceType: "Quran",
                        SourceName: "Qur'an",
                        Reference: $"{v.SurahNumber}:{v.AyahNumber}",
                        PrimaryText: v.ArabicCleaned,
                        OriginalLanguage: "ar",
                        Translations: v.Translations
                            .Select(t => new TranslationDto { Language = t.Language, Translator = t.Translator, Text = t.Text })
                            .ToList(),
                        Metadata: meta
                    )
                    {
                        Score = 100
                    };
                    candidate.Reasons.Add("Exact reference lookup");

                    return _evidenceBuilder.BuildItem(candidate);
                }
            }
        }

        return null;
    }

    public Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string prefix, CancellationToken cancellationToken)
    {
        return Task.FromResult(_suggestionIndex.Search(prefix));
    }
}
