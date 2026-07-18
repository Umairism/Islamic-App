using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Infrastructure.Research.Analysis.Methodologies;

public class ComparativeMethodology : ResearchMethodologyBase
{
    public override ResearchMethodologyType Type => ResearchMethodologyType.Comparative;

    public override IReadOnlyList<ResearchEvidence> OrderEvidence(IReadOnlyList<ResearchEvidence> evidence)
    {
        // Interleave Quran and Hadith references to compare
        var quran = evidence.Where(e => e.Source == EvidenceSource.Quran).OrderByDescending(e => e.RetrievalScore).ToList();
        var hadith = evidence.Where(e => e.Source == EvidenceSource.Hadith).OrderByDescending(e => e.RetrievalScore).ToList();
        
        var interleaved = new List<ResearchEvidence>();
        int i = 0, j = 0;
        while (i < quran.Count || j < hadith.Count)
        {
            if (i < quran.Count) interleaved.Add(quran[i++]);
            if (j < hadith.Count) interleaved.Add(hadith[j++]);
        }
        return interleaved;
    }

    public override IReadOnlyList<string> GetRequiredOutputSections()
    {
        return new List<string> { "Introduction", "Quranic Textual Foundation", "Hadith Parallel Assertions", "Comparative Analysis", "Conclusion" };
    }
}
