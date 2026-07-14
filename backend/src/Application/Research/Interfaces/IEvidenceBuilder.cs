using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface IEvidenceBuilder
{
    EvidenceItem BuildItem(EvidenceMatch match);
    ResearchEvidenceItem BuildResearchItem(EvidenceMatch match, List<CrossReferenceItem> crossRefs);
}
