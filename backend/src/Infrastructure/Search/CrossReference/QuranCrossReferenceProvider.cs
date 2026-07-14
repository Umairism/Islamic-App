using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.CrossReference;

public class QuranCrossReferenceProvider : ICrossReferenceProvider
{
    public EvidenceSource Source => EvidenceSource.Quran;

    public Task<List<CrossReferenceItem>> GetReferencesAsync(string reference, CancellationToken cancellationToken)
    {
        var refs = new List<CrossReferenceItem>();

        // Scenario: Ayat al-Kursi (2:255)
        if (reference == "2:255")
        {
            refs.Add(new CrossReferenceItem(
                Source: EvidenceSource.Quran,
                Reference: "3:2",
                Relationship: EvidenceRelationshipType.Similar,
                Description: "Contains the same divine attributes of Al-Hayy and Al-Qayyum."
            ));

            refs.Add(new CrossReferenceItem(
                Source: EvidenceSource.Quran,
                Reference: "59:23",
                Relationship: EvidenceRelationshipType.Similar,
                Description: "Lists the names and majestic attributes of God."
            ));

            refs.Add(new CrossReferenceItem(
                Source: EvidenceSource.Quran,
                Reference: "2:256",
                Relationship: EvidenceRelationshipType.Contextualizes,
                Description: "No compulsion in religion follows the majesty of Al-Kursi."
            ));
        }
        else if (reference.StartsWith("2:285") || reference.StartsWith("2:286"))
        {
            // Sibling references for safe ranges
            refs.Add(new CrossReferenceItem(
                Source: EvidenceSource.Quran,
                Reference: "2:285",
                Relationship: EvidenceRelationshipType.Repeats,
                Description: "Belief in messengers and divine protection requests."
            ));
        }

        return Task.FromResult(refs);
    }
}
