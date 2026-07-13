using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface IEvidenceBuilder
{
    EvidenceItem BuildItem(SearchCandidate candidate);
}
