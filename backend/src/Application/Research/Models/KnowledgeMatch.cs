using System.Collections.Generic;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

public record RankingContribution(
    RankingFactor Factor,
    double Weight,
    double Value,
    double Contribution
);

public record RankingScore(
    double FinalValue,
    IReadOnlyList<RankingContribution> Contributions
);

public record KnowledgeDocument(
    string Id,
    EvidenceSource Source,
    string Collection,
    ResearchReference Reference,
    string PrimaryText,
    IReadOnlyList<TranslationDto> Translations,
    string DatasetId,
    string ImportSessionId
);

public record KnowledgeMatch(
    KnowledgeDocument Document,
    IReadOnlyList<string> MatchedTokens,
    RankingScore Ranking
);
