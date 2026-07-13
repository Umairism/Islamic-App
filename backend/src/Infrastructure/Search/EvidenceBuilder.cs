using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class EvidenceBuilder : IEvidenceBuilder
{
    private readonly IHighlightBuilder _highlightBuilder;
    private readonly ICitationFormatter _citationFormatter;

    public EvidenceBuilder(IHighlightBuilder highlightBuilder, ICitationFormatter citationFormatter)
    {
        _highlightBuilder = highlightBuilder;
        _citationFormatter = citationFormatter;
    }

    public EvidenceItem BuildItem(EvidenceMatch match)
    {
        var highlights = new List<string>();

        // Generate highlights from each translation matching terms
        foreach (var trans in match.Translations)
        {
            var matchHighlights = _highlightBuilder.BuildHighlights(trans.Text, match.MatchedTerms);
            if (matchHighlights != null && matchHighlights.Count > 0)
            {
                highlights.AddRange(matchHighlights);
            }
        }

        // De-duplicate highlights
        var finalHighlights = highlights.Distinct().Take(3).ToList();

        // Construct standard localized citation using citation formatter strategy orchestrator
        string book = null;
        string verseOrHadithNum = match.Reference;

        if (match.Reference.Contains(":"))
        {
            var parts = match.Reference.Split(':');
            book = parts[0];
            verseOrHadithNum = parts[1];
        }

        var identifier = new KnowledgeIdentifier(
            Source: match.Source,
            Collection: match.Source == EvidenceSource.Quran ? "Quran" : match.Collection,
            Book: book,
            Chapter: "1", // Default chapter index relation placeholder
            VerseOrHadithNumber: verseOrHadithNum,
            Language: "en"
        );

        string formattedCitation = _citationFormatter.Format(identifier, match.Metadata);

        return new EvidenceItem(
            Source: match.Source,
            Collection: match.Collection,
            Reference: formattedCitation,
            PrimaryText: match.PrimaryText,
            Translations: match.Translations,
            Metadata: match.Metadata,
            Score: match.Score,
            Reasons: match.Reasons,
            Highlights: finalHighlights,
            Related: new List<RelatedEvidence>() // Reserved placeholder for future cross-source semantic mappings
        );
    }
}
