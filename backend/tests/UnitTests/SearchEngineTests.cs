using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Infrastructure.Search;
using IslamicApp.Infrastructure.Search.Citation;
using IslamicApp.Infrastructure.Search.CrossReference;
using IslamicApp.Infrastructure.Search.Export;
using IslamicApp.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Search.Retrieval;
using IslamicApp.Infrastructure.Search.Plugins;

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
        var mockWeights = new MockRankingWeightsProvider();
        var engine = new RankingEngine(mockWeights);

        var request = new SearchRequest(
            Query: "parents",
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran },
            Pagination: new Pagination(1, 10),
            IncludeCrossReferences: false,
            IncludeExplanations: false,
            SemanticSearchEnabled: false
        );

        var query = new NormalizedQuery("parents", "parents", new List<string> { "parents" }, new List<string>(), new List<string>(), new List<string>());
        var analysis = new QueryAnalysis(request, query, ResearchLanguage.English, new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 1.0), null, new List<string>());

        var doc = new KnowledgeDocument(
            Id: "quran-2-83",
            Source: EvidenceSource.Quran,
            Collection: "Quran",
            Reference: new QuranReference(2, 83),
            PrimaryText: "وبالوالدين إحسانا",
            Translations: new List<TranslationDto>
            {
                new() { Language = "en", Translator = "Pickthall", Text = "Be good to parents" }
            },
            DatasetId: "quran-json",
            ImportSessionId: "default"
        );

        var candidate = new KnowledgeMatch(doc, new List<string> { "parents" }, new RankingScore(0, new List<RankingContribution>()));

        var context = new SearchContext(request, analysis)
        {
            Candidates = new List<KnowledgeMatch> { candidate }
        };

        var updatedContext = engine.Rank(context);

        Assert.Single(updatedContext.RankedCandidatesList);
        Assert.Equal(82.0, updatedContext.RankedCandidatesList[0].Ranking.FinalValue); // Exact translation matches get 80 points + 2.0 priority boost
    }

    private class MockRankingWeightsProvider : IRankingWeightsProvider
    {
        public double GetWeight(string factor)
        {
            return factor switch
            {
                "Reference" => 100,
                "Alias" => 95,
                "Arabic" => 90,
                "Translation" => 80,
                "SurahName" => 75,
                "Synonym" => 65,
                "Partial" => 40,
                _ => 0
            };
        }

        public IReadOnlyDictionary<string, double> GetAllWeights() => new Dictionary<string, double>();
    }

    [Fact]
    public void CitationFormatter_FormatsCitationsCorrectly()
    {
        var strategies = new List<ICitationStrategy>
        {
            new QuranCitationStrategy(),
            new HadithCitationStrategy()
        };
        var formatter = new CitationFormatter(strategies);

        // Quran english format check
        var quranRef = new QuranReference(2, 255);
        string resultEn = formatter.Format(quranRef, "en");
        Assert.Equal("Qur'an 2:255", resultEn);

        // Quran arabic format check
        string resultAr = formatter.Format(quranRef, "ar");
        Assert.Equal("سورة البقرة آية 255", resultAr);

        // Hadith english format check
        var hadithRef = new HadithReference("Sahih al-Bukhari", 1, 54);
        string hadithEn = formatter.Format(hadithRef, "en");
        Assert.Equal("Sahih al-Bukhari Book 1, Hadith 54", hadithEn);

        // Hadith arabic format check
        string hadithAr = formatter.Format(hadithRef, "ar");
        Assert.Equal("صحيح البخاري، كتاب 1، حديث 54", hadithAr);
    }

    [Fact]
    public async Task QuranCrossReferenceProvider_ResolvesReferencesCorrectly()
    {
        var provider = new QuranCrossReferenceProvider();
        var refs = await provider.GetReferencesAsync("2:255", CancellationToken.None);

        Assert.NotEmpty(refs);
        Assert.Contains(refs, r => r.Reference == "3:2" && r.Relationship == EvidenceRelationshipType.Similar);
        Assert.Contains(refs, r => r.Reference == "2:256" && r.Relationship == EvidenceRelationshipType.Contextualizes);
    }

    [Fact]
    public async Task HadithCrossReferenceProvider_ResolvesCitationsCorrectly()
    {
        var provider = new HadithCrossReferenceProvider();
        var refs = await provider.GetReferencesAsync("54", CancellationToken.None);

        Assert.NotEmpty(refs);
        Assert.Contains(refs, r => r.Source == EvidenceSource.Quran && r.Reference == "2:83" && r.Relationship == EvidenceRelationshipType.Supports);
    }

    [Fact]
    public void ExportFormatters_ConvertDossierToTargets()
    {
        var dossier = new ResearchDossier(
            Query: "parents",
            Summary: "Summary of parent rights",
            EvidenceSections: new Dictionary<EvidenceSection, List<ResearchEvidenceItem>>
            {
                { EvidenceSection.Primary, new List<ResearchEvidenceItem>() }
            },
            PipelineTimeline: new List<PipelineProfilerStep>(),
            Diagnostics: new SearchDiagnostics(),
            ProvenanceList: new List<ResearchProvenance>(),
            ExportMetadata: new ExportMetadata(DateTime.UtcNow, Guid.NewGuid(), "1.0", "v1", 10.0, new List<string>(), "en")
        );

        var formatters = new List<IExportFormatter>
        {
            new JsonExportFormatter(),
            new MarkdownExportFormatter(),
            new HtmlExportFormatter()
        };
        var engine = new ExportEngine(formatters);

        string markdownOutput = engine.Export(dossier, "markdown");
        Assert.Contains("# Research Dossier: parents", markdownOutput);

        string jsonOutput = engine.Export(dossier, "json");
        Assert.Contains("\"query\": \"parents\"", jsonOutput);

        string htmlOutput = engine.Export(dossier, "html");
        Assert.Contains("<h1>Research Dossier: parents</h1>", htmlOutput);
    }
}
