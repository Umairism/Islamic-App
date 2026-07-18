using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IConflictRule
{
    EvidenceConflict? Evaluate(EvidenceGraph graph, QueryAnalysis query);
}
