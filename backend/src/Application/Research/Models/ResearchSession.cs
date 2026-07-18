using System;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

public record ResearchSession(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string Query,
    DateTimeOffset CreatedAt,
    ResearchMethodologyType Methodology,
    ResearchLanguage Language,
    ConfidenceScore Confidence,
    string Status
);
