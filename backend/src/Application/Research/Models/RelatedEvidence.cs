namespace IslamicApp.Application.Research.Models;

public record RelatedEvidence(
    KnowledgeIdentifier Source,
    string RelationshipType, // e.g. "explains", "supports", "referenced_by"
    KnowledgeIdentifier Target
);
