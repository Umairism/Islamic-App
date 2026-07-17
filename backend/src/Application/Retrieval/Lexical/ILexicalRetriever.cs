using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Retrieval.Lexical;

public record LexicalSearchParameters(
    NormalizedQuery Query,
    ResearchLanguage Language,
    ResearchReference? TargetReference,
    IReadOnlySet<EvidenceSource> TargetSources,
    Pagination Pagination
);

public interface ILexicalRetriever
{
    Task<IReadOnlyList<KnowledgeDocument>> QueryLexicalDocumentsAsync(
        LexicalSearchParameters parameters,
        CancellationToken cancellationToken);
    
    Task<KnowledgeDocument?> GetDocumentByReferenceAsync(
        ResearchReference reference,
        CancellationToken cancellationToken);
}
