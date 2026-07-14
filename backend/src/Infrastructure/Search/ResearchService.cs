using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Research.Enums;
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
    private readonly ICrossReferenceEngine _crossReferenceEngine;

    public ResearchService(
        ISearchPipeline pipeline,
        ISearchNormalizer normalizer,
        ITokenizer tokenizer,
        ISynonymEngine synonymEngine,
        ISourceReferenceResolver resolver,
        IRankingConfiguration rankingConfig,
        IEvidenceBuilder evidenceBuilder,
        ApplicationDbContext dbContext,
        SuggestionIndex suggestionIndex,
        ICrossReferenceEngine crossReferenceEngine)
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
        _crossReferenceEngine = crossReferenceEngine;
    }

    public async Task<EvidenceDossier> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var totalSw = Stopwatch.StartNew();
        var context = new SearchContext(query, query.Options);

        // Execute Search Pipeline
        context = await _pipeline.ExecuteAsync(context, cancellationToken);
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
            Language: context.UniqueTokensList.Count > 0 ? "en" : "ar",
            Strategy: context.ResolvedReference != null ? "ReferenceMatch" : "LexicalSearch",
            RankingChecksum: rankingChk,
            SynonymChecksum: synonymChk,
            AliasChecksum: aliasChk,
            StopwordChecksum: tokenizerChk
        );

        var diagnostics = context.DiagnosticsValue with 
        { 
            ExecutionTimeMs = totalSw.Elapsed.TotalMilliseconds 
        };

        // Segment matches into EvidenceCollection groupings
        var collections = new List<EvidenceCollection>();
        var primaryItems = context.EvidenceItemsList.Where(e => e.Score >= 50).ToList();
        var supportingItems = context.EvidenceItemsList.Where(e => e.Score < 50).ToList();

        if (context.ResolvedReference != null)
        {
            collections.Add(new EvidenceCollection("Primary Evidence", context.EvidenceItemsList.ToList()));
        }
        else
        {
            if (primaryItems.Count > 0)
            {
                collections.Add(new EvidenceCollection("Primary Evidence", primaryItems));
            }
            if (supportingItems.Count > 0)
            {
                collections.Add(new EvidenceCollection("Supporting Evidence", supportingItems));
            }
        }

        string strategy = context.ResolvedReference != null ? "Reference" : "Keyword";

        var export = new ExportMetadata(
            GeneratedAt: startedAt,
            SearchId: searchId,
            ApplicationVersion: "1.0",
            DatasetVersions: "Quran-v1,Hadith-v1",
            ExecutionTimeMs: totalSw.Elapsed.TotalMilliseconds,
            SourcesUsed: new List<string> { "Quran", "Hadith" },
            Language: execContext.Language
        );

        var relatedRefs = context.EvidenceItemsList
            .Select(e => e.Reference)
            .Take(5)
            .ToList();

        var relatedTopics = context.UniqueTokensList
            .Concat(context.ExpandedTokensList)
            .Distinct()
            .Take(5)
            .ToList();

        return new EvidenceDossier(
            ExecutionContext: execContext,
            Summary: $"Search strategy: {strategy}. Total matches: {diagnostics.TotalMatches}.",
            Collections: collections,
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
            if (resolved.Identifier.Source == EvidenceSource.Quran && resolved.Identifier.Book != null)
            {
                int surah = int.Parse(resolved.Identifier.Book);
                int ayah = int.Parse(resolved.Identifier.VerseOrHadithNumber ?? "1");

                var v = await _dbContext.QuranVerses
                    .AsNoTracking()
                    .Include(v => v.Surah)
                    .Include(v => v.Translations)
                    .FirstOrDefaultAsync(v => v.SurahNumber == surah && v.AyahNumber == ayah, cancellationToken);

                if (v != null)
                {
                    var match = new EvidenceMatch
                    {
                        Source = EvidenceSource.Quran,
                        Collection = "Quran",
                        Reference = $"{v.SurahNumber}:{v.AyahNumber}",
                        PrimaryText = v.ArabicCleaned,
                        Translations = v.Translations
                            .Select(t => new TranslationDto { Language = t.Language, Translator = t.Translator, Text = t.Text })
                            .ToList(),
                        Metadata = new EvidenceMetadata(
                            Dataset: "Quran-JSON",
                            Edition: "Sahih International",
                            Translator: "Sahih International",
                            Language: "en",
                            Version: "3.1.2",
                            Checksum: "default_checksum"
                        ),
                        Score = 100
                    };
                    match.Reasons.Add("Exact reference lookup");

                    return _evidenceBuilder.BuildItem(match);
                }
            }
            else if (resolved.Identifier.Source == EvidenceSource.Hadith)
            {
                string coll = resolved.Identifier.Collection;
                int hadithNum = int.Parse(resolved.Identifier.VerseOrHadithNumber ?? "1");

                var h = await _dbContext.Hadiths
                    .AsNoTracking()
                    .Include(h => h.Collection)
                    .Include(h => h.Book)
                    .FirstOrDefaultAsync(h => h.Collection.ShortName.Contains(coll) && h.HadithNumber == hadithNum, cancellationToken);

                if (h != null)
                {
                    var match = new EvidenceMatch
                    {
                        Source = EvidenceSource.Hadith,
                        Collection = h.Collection.DisplayName,
                        Reference = $"{h.Book.BookNumber}:{h.HadithNumber}",
                        PrimaryText = h.ArabicCleaned,
                        Translations = new List<TranslationDto>
                        {
                            new() { Language = "en", Translator = "Muhsin Khan", Text = $"{h.EnglishNarrator} {h.EnglishText}" }
                        },
                        Metadata = new EvidenceMetadata(
                            Dataset: h.Collection.DisplayName,
                            Edition: "Darussalam",
                            Translator: "Muhsin Khan",
                            Language: "en",
                            Version: "2025.01",
                            Checksum: "default_checksum"
                        ),
                        Score = 100
                    };
                    match.Reasons.Add("Exact reference lookup");

                    return _evidenceBuilder.BuildItem(match);
                }
            }
        }

        return null;
    }

    public Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string prefix, CancellationToken cancellationToken)
    {
        return Task.FromResult(_suggestionIndex.Search(prefix));
    }

    public async Task<ResearchDossier> ResearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var totalSw = Stopwatch.StartNew();
        var context = new SearchContext(query, query.Options);

        // Execute Search Pipeline with automated memory/time execution steps profiling
        var profilerResult = await _pipeline.ExecuteWithProfilingAsync(context, cancellationToken);
        var finalContext = profilerResult.Context;
        totalSw.Stop();

        // Resolve and map candidates to ResearchEvidenceItem with dynamically fetched cross-references
        var primaryList = new List<ResearchEvidenceItem>();
        var supportingList = new List<ResearchEvidenceItem>();
        var backgroundList = new List<ResearchEvidenceItem>();
        var contrastingList = new List<ResearchEvidenceItem>();
        var commentaryList = new List<ResearchEvidenceItem>();

        var page = query.Options.Page;
        var pageSize = query.Options.PageSize;

        var paginatedCandidates = finalContext.RankedCandidatesList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var candidate in paginatedCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Fetch dynamic cross-references using the ICrossReferenceEngine provider chain
            var crossRefs = await _crossReferenceEngine.ResolveReferencesAsync(candidate.Source, candidate.Reference, cancellationToken);

            // Construct final ResearchEvidenceItem wrapping confidence and structured explanations
            var item = _evidenceBuilder.BuildResearchItem(candidate, crossRefs);

            // Classify item into target dossier section based on relevance score thresholds
            if (finalContext.ResolvedReference != null)
            {
                primaryList.Add(item);
            }
            else
            {
                if (item.Confidence.RankingScore >= 80)
                {
                    primaryList.Add(item);
                }
                else if (item.Confidence.RankingScore >= 50)
                {
                    supportingList.Add(item);
                }
                else
                {
                    backgroundList.Add(item);
                }
            }
        }

        var sections = new Dictionary<EvidenceSection, List<ResearchEvidenceItem>>
        {
            { EvidenceSection.Primary, primaryList },
            { EvidenceSection.Supporting, supportingList },
            { EvidenceSection.Background, backgroundList },
            { EvidenceSection.Contrasting, contrastingList },
            { EvidenceSection.Commentary, commentaryList }
        };

        // Query provenance from active dataset metadata
        var datasetIds = finalContext.RankedCandidatesList.Select(c => c.DatasetId).Distinct().ToList();
        var provenanceList = new List<ResearchProvenance>();

        foreach (var dId in datasetIds)
        {
            string searchId = string.IsNullOrEmpty(dId) ? "quran-json" : dId;
            var dataset = await _dbContext.Datasets
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id.Contains(searchId) || d.Id.StartsWith("quran"), cancellationToken);
            
            if (dataset != null)
            {
                provenanceList.Add(new ResearchProvenance(
                    DatasetId: dataset.Id,
                    ImportSessionId: "default-session",
                    DatasetName: dataset.Name,
                    Version: dataset.Version,
                    Checksum: dataset.Checksum
                ));
            }
            else
            {
                // Fallback for mock/test runs without DB seed
                provenanceList.Add(new ResearchProvenance(
                    DatasetId: searchId,
                    ImportSessionId: "default-session",
                    DatasetName: "Quran-JSON",
                    Version: "3.1.2",
                    Checksum: "default_checksum"
                ));
            }
        }

        string tokenizerChk = (_tokenizer as Tokenizer)?.Checksum ?? "n/a";
        string synonymChk = (_synonymEngine as SynonymEngine)?.Checksum ?? "n/a";
        var searchGuid = Guid.NewGuid();

        var exportMeta = new ExportMetadata(
            GeneratedAt: DateTime.UtcNow,
            SearchId: searchGuid,
            ApplicationVersion: "1.0",
            DatasetVersions: string.Join(",", provenanceList.Select(p => p.Version)),
            ExecutionTimeMs: totalSw.Elapsed.TotalMilliseconds,
            SourcesUsed: finalContext.RankedCandidatesList.Select(c => c.Source.ToString()).Distinct().ToList(),
            Language: "en"
        );

        var finalDiagnostics = finalContext.DiagnosticsValue with 
        { 
            ExecutionTimeMs = totalSw.Elapsed.TotalMilliseconds,
            ReturnedMatches = paginatedCandidates.Count
        };

        return new ResearchDossier(
            Query: query.OriginalQuery,
            Summary: $"Retrieved {finalDiagnostics.TotalMatches} matches in {finalDiagnostics.ExecutionTimeMs:F2}ms using dynamically generated cross-references.",
            EvidenceSections: sections,
            PipelineTimeline: profilerResult.Timeline,
            Diagnostics: finalDiagnostics,
            ProvenanceList: provenanceList,
            ExportMetadata: exportMeta
        );
    }
}
