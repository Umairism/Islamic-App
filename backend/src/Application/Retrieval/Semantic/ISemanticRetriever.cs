using System.Collections.Generic;
using System.Threading.Tasks;
using IslamicApp.Application.Retrieval.Diagnostics;
using IslamicApp.Application.Retrieval.Hybrid;

namespace IslamicApp.Application.Retrieval.Semantic;

public interface ISemanticRetriever
{
    Task<List<CandidateDocument>> RetrieveAsync(RetrievalContext context);
}
