using System.Collections.Generic;

namespace IslamicApp.Application.Retrieval.Hybrid;

public interface IFusionStrategy
{
    List<CandidateDocument> Fuse(
        List<CandidateDocument> lexicalCandidates, 
        List<CandidateDocument> semanticCandidates);
}
