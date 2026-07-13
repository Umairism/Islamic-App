using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ICitationStrategy
{
    EvidenceSource Source { get; }
    string Format(KnowledgeIdentifier identifier, EvidenceMetadata metadata);
}
