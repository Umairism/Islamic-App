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

    public ResearchEvidenceItem BuildResearchItem(EvidenceMatch match, List<CrossReferenceItem> crossRefs)
    {
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
            Chapter: "1",
            VerseOrHadithNumber: verseOrHadithNum,
            Language: "en"
        );

        string formattedCitation = _citationFormatter.Format(identifier, match.Metadata);

        string datasetId = string.IsNullOrEmpty(match.DatasetId) ? 
            (match.Source == EvidenceSource.Quran ? "quran-json" : "hadith-sahih-al-bukhari") : match.DatasetId;
        string importSessionId = string.IsNullOrEmpty(match.ImportSessionId) ? "default-session" : match.ImportSessionId;

        var confidence = match.Confidence ?? new EvidenceConfidence(
            SourceAuthority: match.Source == EvidenceSource.Quran ? "Primary (Divine Revelation)" : "Secondary (Authentic Narration)",
            TextMatch: 0,
            ReferenceMatch: 0,
            RankingScore: match.Score,
            OverallConfidence: 50.0
        );

        var explanation = match.Explanation ?? new SearchExplanation(
            TokenMatches: match.MatchedTerms,
            ReferenceMatches: new List<string>(),
            Boosts: new List<string>(),
            Penalties: new List<string>(),
            RankingFactors: new Dictionary<string, double>()
        );

        return new ResearchEvidenceItem(
            Source: match.Source,
            Collection: match.Collection,
            Reference: formattedCitation,
            PrimaryText: match.PrimaryText,
            Translations: match.Translations,
            DatasetId: datasetId,
            ImportSessionId: importSessionId,
            Confidence: confidence,
            Explanation: explanation,
            CrossReferences: crossRefs ?? new List<CrossReferenceItem>()
        );
    }
}
