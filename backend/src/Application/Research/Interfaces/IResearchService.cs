using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface IResearchService
{
    Task<EvidenceDossier> SearchAsync(SearchQuery query, CancellationToken cancellationToken);
    Task<ResearchDossier> ResearchAsync(SearchQuery query, CancellationToken cancellationToken);
    Task<EvidenceItem?> GetReferenceAsync(string reference, CancellationToken cancellationToken);
    Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string prefix, CancellationToken cancellationToken);
}
