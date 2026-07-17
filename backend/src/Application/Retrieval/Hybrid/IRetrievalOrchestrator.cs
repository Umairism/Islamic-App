using System.Collections.Generic;
using System.Threading.Tasks;
using IslamicApp.Application.Retrieval.Diagnostics;

namespace IslamicApp.Application.Retrieval.Hybrid;

public interface IRetrievalOrchestrator
{
    Task<(List<CandidateDocument> Candidates, RetrievalContext UpdatedContext)> RetrieveCandidatesAsync(
        RetrievalContext context);
}
