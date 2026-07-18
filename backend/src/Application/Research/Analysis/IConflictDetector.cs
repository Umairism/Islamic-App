using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IConflictDetector
{
    ConflictAnalysis DetectConflicts(EvidenceGraph graph, QueryAnalysis analysis);
}
