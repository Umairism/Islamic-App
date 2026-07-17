using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

public record Pagination(int Page, int PageSize);

public record SearchRequest(
    string Query,
    ResearchLanguage Language,
    IReadOnlySet<EvidenceSource> Sources,
    Pagination Pagination,
    bool IncludeCrossReferences,
    bool IncludeExplanations,
    bool SemanticSearchEnabled
);
