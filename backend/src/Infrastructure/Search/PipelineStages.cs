using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using IslamicApp.Application.Research.Catalog;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class NormalizeStage : ISearchPipelineStage
{
    private readonly ISearchNormalizer _normalizer;

    public NormalizeStage(ISearchNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        string normalized = _normalizer.Normalize(context.Query.OriginalQuery);
        sw.Stop();
        
        var updatedDiagnostics = context.DiagnosticsValue with 
        { 
            NormalizationTimeMs = sw.Elapsed.TotalMilliseconds 
        };
        
        return Task.FromResult(context with 
        { 
            NormalizedQuery = normalized,
            Diagnostics = updatedDiagnostics
        });
    }
}

public class TokenizeStage : ISearchPipelineStage
{
    private readonly ITokenizer _tokenizer;

    public TokenizeStage(ITokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var rawTokens = _tokenizer.Tokenize(context.Query.OriginalQuery);
        var normalizedTokens = _tokenizer.Tokenize(context.NormalizedQuery);
        
        string detectedLanguage = DetermineLanguage(context.NormalizedQuery);
        var uniqueTokens = _tokenizer.RemoveStopwords(normalizedTokens, detectedLanguage)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(context with
        {
            RawTokens = rawTokens,
            NormalizedTokens = normalizedTokens,
            UniqueTokens = uniqueTokens
        });
    }

    private static string DetermineLanguage(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return "en";
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

    public Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var expanded = _synonymEngine.ExpandTokens(context.UniqueTokensList.ToList(), out _);
        return Task.FromResult(context with
        {
            ExpandedTokens = expanded
        });
    }
}

public class ReferenceResolutionStage : ISearchPipelineStage
{
    private readonly ISourceReferenceResolver _resolver;

    public ReferenceResolutionStage(ISourceReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        EvidenceReference? reference = null;
        if (_resolver.TryResolve(context.Query.OriginalQuery, out var resolved))
        {
            reference = resolved;
        }
        
        return Task.FromResult(context with
        {
            ResolvedReference = reference
        });
    }
}

public class DatabaseQueryStage : ISearchPipelineStage
{
    private readonly KnowledgeCatalog _catalog;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseQueryStage(KnowledgeCatalog catalog, IServiceProvider serviceProvider)
    {
        _catalog = catalog;
        _serviceProvider = serviceProvider;
    }

    public async Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var matches = new List<EvidenceMatch>();

        // Execute active source searchers in parallel strictly respecting CancellationToken,
        // resolving each searcher inside a dedicated DI scope to ensure thread-safety of DbContext.
        var searchTasks = _catalog.Searchers
            .Select(async searcher =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedSearchers = scope.ServiceProvider.GetRequiredService<IEnumerable<ISourceSearcher>>();
                var scopedSearcher = scopedSearchers.First(s => s.Source == searcher.Source);
                return await scopedSearcher.SearchAsync(context, cancellationToken);
            })
            .ToList();

        var searchResults = await Task.WhenAll(searchTasks);
        foreach (var results in searchResults)
        {
            if (results != null)
            {
                matches.AddRange(results);
            }
        }

        sw.Stop();
        
        var updatedDiagnostics = context.DiagnosticsValue with 
        { 
            QueryTimeMs = sw.Elapsed.TotalMilliseconds,
            TotalMatches = matches.Count
        };

        return context with
        {
            Candidates = matches,
            Diagnostics = updatedDiagnostics
        };
    }
}

public class RankingStage : ISearchPipelineStage
{
    private readonly IRankingEngine _rankingEngine;

    public RankingStage(IRankingEngine rankingEngine)
    {
        _rankingEngine = rankingEngine;
    }

    public Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var updatedContext = _rankingEngine.Rank(context);
        sw.Stop();
        
        var updatedDiagnostics = updatedContext.DiagnosticsValue with 
        { 
            RankingTimeMs = sw.Elapsed.TotalMilliseconds 
        };
        
        return Task.FromResult(updatedContext with 
        { 
            Diagnostics = updatedDiagnostics
        });
    }
}

public class HighlightStage : ISearchPipelineStage
{
    public Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context);
    }
}

public class EvidenceBuildStage : ISearchPipelineStage
{
    private readonly IEvidenceBuilder _evidenceBuilder;

    public EvidenceBuildStage(IEvidenceBuilder evidenceBuilder)
    {
        _evidenceBuilder = evidenceBuilder;
    }

    public Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        
        var page = context.Options.Page;
        var pageSize = context.Options.PageSize;

        var paginatedMatches = context.RankedCandidatesList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var evidenceItems = new List<EvidenceItem>();
        foreach (var match in paginatedMatches)
        {
            evidenceItems.Add(_evidenceBuilder.BuildItem(match));
        }

        sw.Stop();

        var updatedDiagnostics = context.DiagnosticsValue with 
        { 
            EvidenceBuildTimeMs = sw.Elapsed.TotalMilliseconds,
            ReturnedMatches = evidenceItems.Count
        };

        return Task.FromResult(context with
        {
            EvidenceItems = evidenceItems,
            Diagnostics = updatedDiagnostics
        });
    }
}
