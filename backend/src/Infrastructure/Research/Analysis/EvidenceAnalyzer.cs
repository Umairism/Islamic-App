using System;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class EvidenceAnalyzer : IEvidenceAnalyzer
{
    public EvidenceClassification Classify(ResearchEvidence evidence)
    {
        var title = evidence.Title.ToLowerInvariant();
        var content = evidence.Content.ToLowerInvariant();

        if (title.Contains("tafsir") || title.Contains("commentary") || title.Contains("ibn kathir"))
        {
            return EvidenceClassification.Commentary;
        }

        if (title.Contains("lexical") || title.Contains("definition") || title.Contains("meaning") || title.Contains("root"))
        {
            return EvidenceClassification.Linguistic;
        }

        if (title.Contains("reason") || title.Contains("cause") || title.Contains("asbab") || title.Contains("nuzul") || title.Contains("context"))
        {
            return EvidenceClassification.HistoricalContext;
        }

        if (evidence.Source == EvidenceSource.Quran)
        {
            return EvidenceClassification.PrimarySource;
        }

        if (evidence.Source == EvidenceSource.Hadith)
        {
            // Hadith acts as primary proof, or secondary interpretation source depending on narration strength
            return EvidenceClassification.SecondarySource;
        }

        return EvidenceClassification.Example;
    }
}
