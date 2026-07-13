using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Catalog;

public record KnowledgeSourceDescriptor(
    EvidenceSource Source,
    string DisplayName,
    string Version,
    IReadOnlyList<string> Languages,
    int Priority,
    bool Enabled,
    bool SupportsSearch,
    bool SupportsCitation
);
