using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Catalog;
using IslamicApp.Infrastructure.Search;
using IslamicApp.Infrastructure.Search.Citation;
using IslamicApp.Application.DTOs;

namespace IslamicApp.UnitTests;

public class SearchEngineTests
{
    [Fact]
    public void SearchNormalizer_CleansDiacriticsAndPunctuation()
    {
        var normalizer = new SearchNormalizer();
        string input = "ٱللهُ لَا إِلَهَ إِلَّا هُوَ الْحَيُّ الْقَيُّومُ!";
        string expected = "الله لا اله الا هو الحي القيوم";

        string result = normalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Tokenizer_ExtractsCleanTokensAndFiltersStopwords()
    {
        var tokenizer = new Tokenizer();
        var rawText = "the rights of parents in islam";
        var tokens = tokenizer.Tokenize(rawText);
        
        Assert.Equal(new[] { "the", "rights", "of", "parents", "in", "islam" }, tokens);

        var clean = tokenizer.RemoveStopwords(tokens, "en");
        
        Assert.Contains("rights", clean);
        Assert.Contains("parents", clean);
        Assert.Contains("islam", clean);
        Assert.DoesNotContain("the", clean);
        Assert.DoesNotContain("of", clean);
        Assert.DoesNotContain("in", clean);
    }

    [Fact]
    public void SynonymEngine_ExpandsWeightedSynonyms()
    {
        var engine = new SynonymEngine();
        var tokens = new List<string> { "charity" };

        var expanded = engine.ExpandTokens(tokens, out var weights);

        Assert.Contains("charity", expanded);
        Assert.Contains("zakat", expanded);
        Assert.Contains("sadaqah", expanded);
        
        Assert.Equal(1.0, weights["charity"]);
        Assert.Equal(1.0, weights["zakat"]);
        Assert.Equal(0.8, weights["sadaqah"]);
    }

    [Fact]
    public void SourceReferenceResolver_ResolvesStandardAndAliasNotations()
    {
        var resolver = new SourceReferenceResolver();

        // Ayat al-Kursi Alias
        bool success1 = resolver.TryResolve("ayat al kursi", out var ref1);
        Assert.True(success1);
        Assert.NotNull(ref1);
        Assert.Equal("2:255", ref1.FormattedReference);

        // Standard Notation
        bool success2 = resolver.TryResolve("2:255", out var ref2);
        Assert.True(success2);
        Assert.NotNull(ref2);
        Assert.Equal("2:255", ref2.FormattedReference);

        // Name Notation
        bool success3 = resolver.TryResolve("baqarah 255", out var ref3);
        Assert.True(success3);
        Assert.NotNull(ref3);
        Assert.Equal("2:255", ref3.FormattedReference);

        // Invalid Reference
        bool success4 = resolver.TryResolve("invalid 999", out var ref4);
        Assert.False(success4);
    }

    [Fact]
    public void HighlightBuilder_WrapsMatchedTermsInEmphasisTags()
    {
        var builder = new HighlightBuilder();
        string text = "And We have commanded man to honour his parents.";
        var terms = new List<string> { "parents" };

        var highlights = builder.BuildHighlights(text, terms);

        Assert.Single(highlights);
        Assert.Contains("to honour his <em>parents</em>.", highlights[0]);
    }

    [Fact]
    public void RankingEngine_AssignsScoresCorrectlyByConfig()
    {
        var mockConfig = new MockRankingConfiguration();
        var engine = new RankingEngine(mockConfig);

        var query = new SearchQuery("parents", new SearchOptions());

        var candidate = new EvidenceMatch
        {
            Source = EvidenceSource.Quran,
            Collection = "Quran",
            Reference = "2:83",
            PrimaryText = "وبالوالدين إحسانا",
            Translations = new List<TranslationDto>
            {
                new() { Language = "en", Translator = "Pickthall", Text = "Be good to parents" }
            },
            Metadata = new EvidenceMetadata("Quran-JSON", "Sahih International", "Sahih International", "en", "3.1.2", "default_checksum")
        };

        var context = new SearchContext(query, query.Options)
        {
            NormalizedQuery = "parents",
            UniqueTokens = new List<string> { "parents" },
            ExpandedTokens = new List<string> { "parents" },
            Candidates = new List<EvidenceMatch> { candidate }
        };

        var updatedContext = engine.Rank(context);

        Assert.Single(updatedContext.RankedCandidatesList);
        Assert.Equal(82.0, updatedContext.RankedCandidatesList[0].Score); // Exact translation matches get 80 points + 2.0 priority boost
        Assert.Contains("Exact translation match in en", updatedContext.RankedCandidatesList[0].Reasons);
    }

    private class MockRankingConfiguration : IRankingConfiguration
    {
        public double Reference => 100;
        public double Alias => 95;
        public double Arabic => 90;
        public double Translation => 80;
        public double SurahName => 75;
         public double Synonym => 65;
        public double Partial => 40;
        public string Checksum => "mock_checksum";
    }

    [Fact]
    public void CitationFormatter_FormatsCitationsCorrectly()
    {
        var strategies = new List<ICitationStrategy>
        {
            new QuranCitationStrategy(),
            new HadithCitationStrategy()
        };
        var catalog = new KnowledgeCatalog(new List<ISourceSearcher>(), strategies);
        var formatter = new CitationFormatter(catalog);

        // Quran english format check
        var quranIdentEn = new KnowledgeIdentifier(EvidenceSource.Quran, "Quran", "2", null, "255", "en");
        string resultEn = formatter.Format(quranIdentEn, new EvidenceMetadata("Quran-JSON", "Sahih International", "Sahih International", "en", "3.1.2", "chk"));
        Assert.Equal("Qur'an 2:255", resultEn);

        // Quran arabic format check
        var quranIdentAr = new KnowledgeIdentifier(EvidenceSource.Quran, "Quran", "2", null, "255", "ar");
        string resultAr = formatter.Format(quranIdentAr, new EvidenceMetadata("Quran-JSON", "Sahih International", "Sahih International", "ar", "3.1.2", "chk"));
        Assert.Equal("سورة البقرة آية 255", resultAr);

        // Hadith english format check
        var hadithIdentEn = new KnowledgeIdentifier(EvidenceSource.Hadith, "Sahih al-Bukhari", "1", "1", "54", "en");
        string hadithEn = formatter.Format(hadithIdentEn, new EvidenceMetadata("Bukhari", "Darussalam", "Muhsin Khan", "en", "2025.01", "chk"));
        Assert.Equal("Sahih al-Bukhari Book 1, Hadith 54", hadithEn);

        // Hadith arabic format check
        var hadithIdentAr = new KnowledgeIdentifier(EvidenceSource.Hadith, "صحيح البخاري", "1", "1", "54", "ar");
        string hadithAr = formatter.Format(hadithIdentAr, new EvidenceMetadata("Bukhari", "Darussalam", "Muhsin Khan", "ar", "2025.01", "chk"));
        Assert.Equal("صحيح البخاري، كتاب 1، حديث 54", hadithAr);
    }
}
