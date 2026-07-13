using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class EvidenceBuilder : IEvidenceBuilder
{
    private readonly IHighlightBuilder _highlightBuilder;

    public EvidenceBuilder(IHighlightBuilder highlightBuilder)
    {
        _highlightBuilder = highlightBuilder;
    }

    public EvidenceItem BuildItem(SearchCandidate candidate)
    {
        var highlights = new List<string>();

        // Generate highlights from each translation matching terms
        foreach (var trans in candidate.Translations)
        {
            var matchHighlights = _highlightBuilder.BuildHighlights(trans.Text, candidate.MatchedTerms);
            if (matchHighlights != null && matchHighlights.Count > 0)
            {
                highlights.AddRange(matchHighlights);
            }
        }

        // De-duplicate highlights and limit
        var finalHighlights = highlights.Distinct().Take(3).ToList();

        return new EvidenceItem(
            SourceType: candidate.SourceType,
            SourceName: candidate.SourceName,
            Reference: candidate.Reference,
            PrimaryText: candidate.PrimaryText,
            OriginalLanguage: candidate.OriginalLanguage,
            Translations: candidate.Translations,
            Metadata: candidate.Metadata,
            Score: candidate.Score,
            Reasons: candidate.Reasons,
            Highlights: finalHighlights
        );
    }
}
