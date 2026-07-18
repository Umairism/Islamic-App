using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IResearchAnalysisBuilder
{
    ResearchAnalysis Build(
        QueryAnalysis query,
        EvidenceCorpus corpus);
}
