using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ICitationFormatter
{
    string Format(KnowledgeIdentifier identifier, EvidenceMetadata metadata);
}
