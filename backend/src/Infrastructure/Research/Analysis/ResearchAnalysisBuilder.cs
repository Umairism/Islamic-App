using System;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class ResearchAnalysisBuilder : IResearchAnalysisBuilder
{
    private readonly IMethodologySelector _methodologySelector;
    private readonly IResearchMethodologyFactory _methodologyFactory;
    private readonly IGraphBuilder _graphBuilder;
    private readonly IConflictDetector _conflictDetector;

    public ResearchAnalysisBuilder(
        IMethodologySelector methodologySelector,
        IResearchMethodologyFactory methodologyFactory,
        IGraphBuilder graphBuilder,
        IConflictDetector conflictDetector)
    {
        _methodologySelector = methodologySelector;
        _methodologyFactory = methodologyFactory;
        _graphBuilder = graphBuilder;
        _conflictDetector = conflictDetector;
    }

    public ResearchAnalysis Build(QueryAnalysis query, EvidenceCorpus corpus)
    {
        var methodType = _methodologySelector.ResolveMethodology(query);
        var methodology = _methodologyFactory.CreateMethodology(methodType);

        var graph = _graphBuilder.BuildGraph(corpus, query);
        var conflicts = _conflictDetector.DetectConflicts(graph, query);

        return new ResearchAnalysis(graph, conflicts, methodology);
    }
}
