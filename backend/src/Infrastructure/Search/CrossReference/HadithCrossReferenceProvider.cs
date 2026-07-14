using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.CrossReference;

public class HadithCrossReferenceProvider : ICrossReferenceProvider
{
    public EvidenceSource Source => EvidenceSource.Hadith;

    public Task<List<CrossReferenceItem>> GetReferencesAsync(string reference, CancellationToken cancellationToken)
    {
        var refs = new List<CrossReferenceItem>();

        // If lookup is on Quran verse 2:255, return Hadith links
        if (reference == "2:255")
        {
            refs.Add(new CrossReferenceItem(
                Source: EvidenceSource.Hadith,
                Reference: "Bukhari Hadith 54",
                Relationship: EvidenceRelationshipType.Mentions,
                Description: "Narrates the context and excellence of Ayat al-Kursi."
            ));
        }
        else if (reference.Contains("54"))
        {
            // If lookup is on Hadith 54, return Quran/Hadith links
            refs.Add(new CrossReferenceItem(
                Source: EvidenceSource.Quran,
                Reference: "2:83",
                Relationship: EvidenceRelationshipType.Supports,
                Description: "Expounds on duties towards parents and relatives mentioned in Quran."
            ));

            refs.Add(new CrossReferenceItem(
                Source: EvidenceSource.Hadith,
                Reference: "Muslim Hadith 12",
                Relationship: EvidenceRelationshipType.Similar,
                Description: "Parallel narration in Sahih Muslim concerning intention."
            ));
        }

        return Task.FromResult(refs);
    }
}
