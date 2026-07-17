using IslamicApp.Application.Research.Models;
using System.Collections.Generic;

namespace IslamicApp.Application.Research.Interfaces;

public interface IEvidenceBuilder
{
    ResearchEvidenceItem BuildResearchItem(KnowledgeMatch match, List<CrossReferenceItem> crossRefs);
}
