using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

public record KnowledgeIdentifier(
    EvidenceSource Source,
    string Collection,
    string? Book,
    string? Chapter,
    string? VerseOrHadithNumber,
    string Language
);
