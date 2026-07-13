using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

public record SearcherCapabilities(
    IReadOnlySet<SearchLanguage> Languages,
    IReadOnlySet<SearchFeature> Features
);
