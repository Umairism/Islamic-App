using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IMethodologySelector
{
    ResearchMethodologyType ResolveMethodology(QueryAnalysis analysis);
}

public interface IResearchMethodologyFactory
{
    IResearchMethodology CreateMethodology(ResearchMethodologyType type);
}
