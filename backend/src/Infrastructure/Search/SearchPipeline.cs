using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class SearchPipeline : ISearchPipeline
{
    private readonly List<ISearchPipelineStage> _stages = new();

    public SearchPipeline(
        NormalizeStage normalizeStage,
        TokenizeStage tokenizeStage,
        SynonymExpansionStage synonymExpansionStage,
        ReferenceResolutionStage referenceResolutionStage,
        DatabaseQueryStage databaseQueryStage,
        RankingStage rankingStage,
        EvidenceBuildStage evidenceBuildStage)
    {
        _stages.Add(normalizeStage);
        _stages.Add(tokenizeStage);
        _stages.Add(synonymExpansionStage);
        _stages.Add(referenceResolutionStage);
        _stages.Add(databaseQueryStage);
        _stages.Add(rankingStage);
        _stages.Add(evidenceBuildStage);
    }

    public async Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var currentContext = context;
        foreach (var stage in _stages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            currentContext = await stage.ExecuteAsync(currentContext, cancellationToken);
        }
        return currentContext;
    }
}
