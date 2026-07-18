using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IGraphBuilder
{
    EvidenceGraph BuildGraph(EvidenceCorpus corpus, QueryAnalysis analysis);
}
