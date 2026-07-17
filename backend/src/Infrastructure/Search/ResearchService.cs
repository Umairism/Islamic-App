using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;

namespace IslamicApp.Infrastructure.Search;

public class ResearchService : IResearchService
{
    private readonly IQueryAnalyzer _queryAnalyzer;
    private readonly ISearchPipeline _pipeline;
    private readonly ApplicationDbContext _dbContext;
    private readonly SuggestionIndex _suggestionIndex;

    public ResearchService(
        IQueryAnalyzer queryAnalyzer,
        ISearchPipeline pipeline,
        ApplicationDbContext dbContext,
        SuggestionIndex suggestionIndex)
    {
        _queryAnalyzer = queryAnalyzer;
        _pipeline = pipeline;
        _dbContext = dbContext;
        _suggestionIndex = suggestionIndex;
    }

    public async Task<EvidenceDossier> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var sources = new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith };
        var pagination = new Pagination(query.Options.Page, query.Options.PageSize);
        var request = new SearchRequest(
            Query: query.OriginalQuery,
            Language: ResearchLanguage.Auto,
            Sources: sources,
            Pagination: pagination,
            IncludeCrossReferences: false,
            IncludeExplanations: false,
            SemanticSearchEnabled: false
        );

        var analysis = await _queryAnalyzer.AnalyzeAsync(request);
        var context = new SearchContext(request, analysis);

        var profilerResult = await _pipeline.ExecuteWithProfilingAsync(context, cancellationToken);
        var finalContext = profilerResult.Context;

        var items = finalContext.ResearchEvidenceItemsList.Select(item =>
        {
            var reasons = new List<string>(item.Explanation?.Boosts ?? new List<string>());
            if (analysis.IsReferenceLookup && analysis.ParsedReference != null)
            {
                bool isAlias = !query.OriginalQuery.Trim().Equals(analysis.ParsedReference.LookupKey, StringComparison.OrdinalIgnoreCase) &&
                               !query.OriginalQuery.Trim().Equals($"{analysis.ParsedReference.Source} {analysis.ParsedReference.LookupKey}", StringComparison.OrdinalIgnoreCase) &&
                               !query.OriginalQuery.Trim().Contains(":");
                reasons.Add(isAlias ? "Alias reference match" : "Exact reference match");
            }
            return new EvidenceItem(
                Source: item.Source,
                Collection: item.Collection,
                Reference: item.Reference,
                PrimaryText: item.PrimaryText,
                Translations: item.Translations,
                Metadata: new EvidenceMetadata(item.Collection, "Edition", "Translator", "en", "1.0", "chk"),
                Score: item.Confidence.RankingScore,
                Reasons: reasons,
                Highlights: new List<string>(),
                Related: new List<RelatedEvidence>()
            );
        }).ToList();

        var primaryItems = new List<EvidenceItem>();
        var supportingItems = new List<EvidenceItem>();

        foreach (var item in items)
        {
            if (analysis.IsReferenceLookup || item.Score >= 80)
            {
                primaryItems.Add(item);
            }
            else
            {
                supportingItems.Add(item);
            }
        }

        var collectionsList = new List<EvidenceCollection>();
        if (primaryItems.Count > 0)
        {
            collectionsList.Add(new EvidenceCollection("Primary Evidence", primaryItems));
        }
        if (supportingItems.Count > 0)
        {
            collectionsList.Add(new EvidenceCollection("Supporting Evidence", supportingItems));
        }

        var execContext = new SearchExecutionContext(
            SearchId: Guid.NewGuid(),
            StartedAt: DateTime.UtcNow,
            OriginalQuery: query.OriginalQuery,
            NormalizedQuery: analysis.Query.Normalized,
            Language: "en",
            Strategy: analysis.IsReferenceLookup ? "ReferenceMatch" : "LexicalSearch",
            RankingChecksum: "default",
            SynonymChecksum: "default",
            AliasChecksum: "default",
            StopwordChecksum: "default"
        );

        var exportMeta = new ExportMetadata(
            GeneratedAt: DateTime.UtcNow,
            SearchId: execContext.SearchId,
            ApplicationVersion: "1.0",
            DatasetVersions: "1.0",
            ExecutionTimeMs: finalContext.DiagnosticsValue.ExecutionTimeMs,
            SourcesUsed: new List<string> { "Quran", "Hadith" },
            Language: "en"
        );

        return new EvidenceDossier(
            ExecutionContext: execContext,
            Summary: $"Retrieved {items.Count} matches in {finalContext.DiagnosticsValue.ExecutionTimeMs:F2}ms.",
            Collections: collectionsList,
            RelatedReferences: new List<string>(),
            RelatedTopics: new List<string>(),
            ExportMetadata: exportMeta
        );
    }

    public async Task<ResearchDossier> ResearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var totalSw = Stopwatch.StartNew();

        // 1. Execute Query Analyzer
        var analysis = await _queryAnalyzer.AnalyzeAsync(request);

        // 2. Execute Search Pipeline with automatic timing profiling
        var context = new SearchContext(request, analysis);
        var profilerResult = await _pipeline.ExecuteWithProfilingAsync(context, cancellationToken);
        var finalContext = profilerResult.Context;
        
        totalSw.Stop();

        // 3. Classify evidence items into target dossier sections
        var primaryList = new List<ResearchEvidenceItem>();
        var supportingList = new List<ResearchEvidenceItem>();
        var backgroundList = new List<ResearchEvidenceItem>();
        var contrastingList = new List<ResearchEvidenceItem>();
        var commentaryList = new List<ResearchEvidenceItem>();

        foreach (var item in finalContext.ResearchEvidenceItemsList)
        {
            if (finalContext.Analysis.IsReferenceLookup)
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

        // 4. Resolve provenance metadata from database
        var provenanceList = new List<ResearchProvenance>();
        var datasetIds = finalContext.RankedCandidatesList.Select(c => c.Document.DatasetId).Distinct().ToList();

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
                provenanceList.Add(new ResearchProvenance(
                    DatasetId: searchId,
                    ImportSessionId: "default-session",
                    DatasetName: "Quran-JSON",
                    Version: "3.1.2",
                    Checksum: "default_checksum"
                ));
            }
        }

        var searchGuid = Guid.NewGuid();
        var exportMeta = new ExportMetadata(
            GeneratedAt: DateTime.UtcNow,
            SearchId: searchGuid,
            ApplicationVersion: "1.0",
            DatasetVersions: string.Join(",", provenanceList.Select(p => p.Version)),
            ExecutionTimeMs: totalSw.Elapsed.TotalMilliseconds,
            SourcesUsed: finalContext.RankedCandidatesList.Select(c => c.Document.Source.ToString()).Distinct().ToList(),
            Language: request.Language.ToString()
        );

        var finalDiagnostics = finalContext.DiagnosticsValue with 
        { 
            ExecutionTimeMs = totalSw.Elapsed.TotalMilliseconds,
            ReturnedMatches = finalContext.ResearchEvidenceItemsList.Count
        };

        return new ResearchDossier(
            Query: request.Query,
            Summary: $"Retrieved {finalDiagnostics.TotalMatches} matches in {finalDiagnostics.ExecutionTimeMs:F2}ms using capability-based dynamic retrieval.",
            EvidenceSections: sections,
            PipelineTimeline: profilerResult.Timeline,
            Diagnostics: finalDiagnostics,
            ProvenanceList: provenanceList,
            ExportMetadata: exportMeta
        );
    }

    public async Task<EvidenceItem?> GetReferenceAsync(string reference, CancellationToken cancellationToken)
    {
        // Helper resolving single reference lookup
        var sources = new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith };
        var request = new SearchRequest(
            Query: reference,
            Language: ResearchLanguage.Auto,
            Sources: sources,
            Pagination: new Pagination(1, 1),
            IncludeCrossReferences: false,
            IncludeExplanations: false,
            SemanticSearchEnabled: false
        );

        var analysis = await _queryAnalyzer.AnalyzeAsync(request);
        if (analysis.ParsedReference != null)
        {
            var context = new SearchContext(request, analysis);
            var result = await _pipeline.ExecuteAsync(context, cancellationToken);
            if (result.ResearchEvidenceItemsList.Count > 0)
            {
                var item = result.ResearchEvidenceItemsList[0];
                return new EvidenceItem(
                    Source: item.Source,
                    Collection: item.Collection,
                    Reference: item.Reference,
                    PrimaryText: item.PrimaryText,
                    Translations: item.Translations,
                    Metadata: new EvidenceMetadata(item.Collection, "Edition", "Translator", "en", "1.0", "chk"),
                    Score: item.Confidence.RankingScore,
                    Reasons: item.Explanation.Boosts,
                    Highlights: new List<string>(),
                    Related: new List<RelatedEvidence>()
                );
            }
        }

        return null;
    }

    public Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string prefix, CancellationToken cancellationToken)
    {
        return Task.FromResult(_suggestionIndex.Search(prefix));
    }
}
