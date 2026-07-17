using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class QueryAnalyzer : IQueryAnalyzer
{
    private readonly ISearchNormalizer _normalizer;
    private readonly ITokenizer _tokenizer;
    private readonly ISynonymProvider _synonymProvider;
    private readonly IAliasProvider _aliasProvider;

    private static readonly Regex QuranRegex = new(@"^(\d+):(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HadithRegex = new(@"^(bukhari|muslim)\s+(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public QueryAnalyzer(ISearchNormalizer normalizer, ITokenizer tokenizer, ISynonymProvider synonymProvider, IAliasProvider aliasProvider)
    {
        _normalizer = normalizer;
        _tokenizer = tokenizer;
        _synonymProvider = synonymProvider;
        _aliasProvider = aliasProvider;
    }

    public Task<QueryAnalysis> AnalyzeAsync(SearchRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        string raw = request.Query?.Trim() ?? string.Empty;
        string normalized = _normalizer.Normalize(raw);
        var tokens = _tokenizer.Tokenize(normalized);

        // Alias check
        string parseTarget = raw;
        if (_aliasProvider.TryResolveAlias(raw, out var resolvedRef) || _aliasProvider.TryResolveAlias(normalized, out resolvedRef))
        {
            parseTarget = resolvedRef;
        }

        // Language Detection: Check for Arabic characters
        var language = request.Language;
        if (language == ResearchLanguage.Auto)
        {
            language = HasArabicCharacters(raw) ? ResearchLanguage.Arabic : ResearchLanguage.English;
        }

        // 1. Reference Parsing & Search Intent Mapping
        ResearchReference? reference = null;
        SearchMode mode = SearchMode.KeywordSearch;
        var sources = new HashSet<EvidenceSource>(request.Sources);
        var capabilities = new HashSet<RetrievalCapability> { RetrievalCapability.PrefixSearch };
        double confidence = 0.5;

        // Try Quran pattern (e.g. 2:255)
        var quranMatch = QuranRegex.Match(parseTarget);
        if (quranMatch.Success && int.TryParse(quranMatch.Groups[1].Value, out int surah) && int.TryParse(quranMatch.Groups[2].Value, out int ayah))
        {
            try
            {
                reference = new QuranReference(surah, ayah);
                mode = SearchMode.ReferenceLookup;
                sources.Clear();
                sources.Add(EvidenceSource.Quran);
                confidence = 1.0;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Invalid reference, keep mode as keyword search
            }
        }

        // Try Hadith pattern (e.g. bukhari 54)
        if (reference == null)
        {
            var hadithMatch = HadithRegex.Match(parseTarget);
            if (hadithMatch.Success)
            {
                string collection = hadithMatch.Groups[1].Value.Equals("bukhari", StringComparison.OrdinalIgnoreCase) 
                    ? "Sahih al-Bukhari" 
                    : "Sahih Muslim";
                if (int.TryParse(hadithMatch.Groups[2].Value, out int hadithNum))
                {
                    try
                    {
                        reference = new HadithReference(collection, 1, hadithNum);
                        mode = SearchMode.ReferenceLookup;
                        sources.Clear();
                        sources.Add(EvidenceSource.Hadith);
                        confidence = 1.0;
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
        }

        // If no references, determine keyword/topic intent
        if (reference == null)
        {
            bool hasSynonyms = false;
            var expandedTokens = new List<string>();
            foreach (var token in tokens)
            {
                var synonyms = _synonymProvider.GetSynonyms(token);
                if (synonyms != null && synonyms.Any())
                {
                    hasSynonyms = true;
                    expandedTokens.AddRange(synonyms);
                }
            }

            if (hasSynonyms)
            {
                capabilities.Add(RetrievalCapability.SynonymExpansion);
            }

            if (tokens.Count > 4)
            {
                mode = SearchMode.NaturalLanguage;
                confidence = 0.8;
            }
            else
            {
                mode = SearchMode.KeywordSearch;
                confidence = 0.95;
            }
        }

        var intent = new QueryIntent(mode, sources, capabilities, confidence);

        var query = new NormalizedQuery(
            Original: raw,
            Normalized: normalized,
            Tokens: tokens,
            Stems: new List<string>(), // Placeholders for Milestone 6B Arabic morphology
            ArabicRoots: new List<string>(),
            Synonyms: new List<string>()
        );

        var analysis = new QueryAnalysis(
            OriginalRequest: request,
            Query: query,
            DetectedLanguage: language,
            Intent: intent,
            ParsedReference: reference,
            ExtractedTopics: new List<string>()
        );

        return Task.FromResult(analysis);
    }

    private static bool HasArabicCharacters(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        // Check unicode range for Arabic block (0x0600 - 0x06FF)
        return text.Any(c => c >= 0x0600 && c <= 0x06FF);
    }
}
