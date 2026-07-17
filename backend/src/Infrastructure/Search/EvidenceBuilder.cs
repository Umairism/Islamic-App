using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class EvidenceBuilder : IEvidenceBuilder
{
    private readonly ICitationFormatter _citationFormatter;

    public EvidenceBuilder(ICitationFormatter citationFormatter)
    {
        _citationFormatter = citationFormatter;
    }

    public ResearchEvidenceItem BuildResearchItem(KnowledgeMatch match, List<CrossReferenceItem> crossRefs)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));

        string language = "en";
        string formattedCitation = _citationFormatter.Format(match.Document.Reference, language);

        string authority = match.Document.Source == EvidenceSource.Quran ? "Primary (Divine Revelation)" : "Secondary (Authentic Narration)";
        
        double textMatch = 0;
        double referenceMatch = 0;

        var refContrib = match.Ranking.Contributions.FirstOrDefault(c => c.Factor == RankingFactor.ExactReference);
        if (refContrib != null) referenceMatch = refContrib.Value * 100.0;

        var textContrib = match.Ranking.Contributions.FirstOrDefault(c => c.Factor == RankingFactor.ExactWord || c.Factor == RankingFactor.Prefix);
        if (textContrib != null) textMatch = textContrib.Value * 100.0;

        double overallConfidence = 0;
        if (referenceMatch == 100) overallConfidence = match.Document.Source == EvidenceSource.Quran ? 98.0 : 92.0;
        else if (referenceMatch == 95) overallConfidence = match.Document.Source == EvidenceSource.Quran ? 95.0 : 88.0;
        else if (textMatch == 100) overallConfidence = match.Document.Source == EvidenceSource.Quran ? 88.0 : 82.0;
        else overallConfidence = Math.Clamp(match.Ranking.FinalValue * 0.9, 0, 100);

        var confidence = new EvidenceConfidence(
            SourceAuthority: authority,
            TextMatch: textMatch,
            ReferenceMatch: referenceMatch,
            RankingScore: match.Ranking.FinalValue,
            OverallConfidence: overallConfidence
        );

        var explanation = new SearchExplanation(
            TokenMatches: match.MatchedTokens.ToList(),
            ReferenceMatches: new List<string> { match.Document.Reference.ToDisplayString() },
            Boosts: match.Ranking.Contributions.Where(c => c.Factor == RankingFactor.CollectionPriority).Select(c => $"Collection priority boost (+{c.Contribution:F1})").ToList(),
            Penalties: new List<string>(),
            RankingFactors: match.Ranking.Contributions.GroupBy(c => c.Factor.ToString()).ToDictionary(g => g.Key, g => g.Max(c => c.Contribution))
        );

        return new ResearchEvidenceItem(
            Source: match.Document.Source,
            Collection: match.Document.Collection,
            Reference: formattedCitation,
            PrimaryText: match.Document.PrimaryText,
            Translations: match.Document.Translations,
            DatasetId: match.Document.DatasetId,
            ImportSessionId: match.Document.ImportSessionId,
            Confidence: confidence,
            Explanation: explanation,
            CrossReferences: crossRefs ?? new List<CrossReferenceItem>()
        );
    }
}
