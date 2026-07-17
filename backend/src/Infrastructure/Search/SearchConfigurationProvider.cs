using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class SearchConfigurationProvider : 
    ISynonymProvider, 
    IAliasProvider, 
    IStopWordProvider, 
    IRankingWeightsProvider
{
    private readonly SearchConfiguration _config;

    public SearchConfigurationProvider()
    {
        var synonyms = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var surahs = new Dictionary<int, string>();
        var filesLoaded = new List<string>();

        // Load synonyms
        string synPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "synonyms.json");
        if (File.Exists(synPath))
        {
            try
            {
                var content = File.ReadAllText(synPath);
                synonyms = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(content) ?? synonyms;
                filesLoaded.Add(synPath);
            }
            catch {}
        }
        else
        {
            synonyms["parents"] = new List<string> { "father", "mother", "parent", "walidayn" };
            synonyms["intention"] = new List<string> { "intent", "niyyah", "purpose" };
        }

        // Load aliases
        string aliasPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aliases.json");
        if (File.Exists(aliasPath))
        {
            try
            {
                var content = File.ReadAllText(aliasPath);
                var itemsList = JsonSerializer.Deserialize<List<AliasConfigItem>>(content);
                if (itemsList != null)
                {
                    foreach (var item in itemsList)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Alias))
                        {
                            aliases[item.Alias] = item.Reference;
                        }
                    }
                }
                filesLoaded.Add(aliasPath);
            }
            catch {}
        }
        else
        {
            aliases["ayat al-kursi"] = "2:255";
            aliases["ayat al kursi"] = "2:255";
            aliases["kursi"] = "2:255";
        }

        // Load ranking weights
        string weightPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ranking-settings.json");
        if (File.Exists(weightPath))
        {
            try
            {
                var content = File.ReadAllText(weightPath);
                weights = JsonSerializer.Deserialize<Dictionary<string, double>>(content) ?? weights;
                filesLoaded.Add(weightPath);
            }
            catch {}
        }
        else
        {
            weights["Reference"] = 100.0;
            weights["Alias"] = 95.0;
            weights["Arabic"] = 90.0;
            weights["Translation"] = 80.0;
            weights["SurahName"] = 75.0;
            weights["Synonym"] = 65.0;
            weights["Partial"] = 40.0;
        }

        // Set stopwords defaults
        stopWords.Add("and");
        stopWords.Add("the");
        stopWords.Add("in");
        stopWords.Add("of");
        stopWords.Add("on");

        // Surah names translation map
        surahs[1] = "Al-Fatihah";
        surahs[2] = "Al-Baqarah";
        surahs[3] = "Ali 'Imran";

        string rawManifestInput = string.Join(",", filesLoaded) + DateTime.UtcNow.Date.Ticks;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawManifestInput));
        string checksum = Convert.ToHexString(hash);

        var manifest = new ConfigurationManifest(
            Version: "1.0.0",
            Checksum: checksum,
            LoadedAt: DateTime.UtcNow,
            SourceFiles: filesLoaded
        );

        _config = new SearchConfiguration(synonyms, aliases, stopWords, weights, surahs, manifest);
    }

    public SearchConfiguration Configuration => _config;

    // ISynonymProvider
    public IReadOnlyCollection<string> GetSynonyms(string word)
    {
        if (_config.Synonyms.TryGetValue(word, out var list)) return list;
        return Array.Empty<string>();
    }

    public bool TryResolveAlias(string alias, out string normalizedReference)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            normalizedReference = null!;
            return false;
        }
        var match = _config.Aliases.FirstOrDefault(a => a.Key.Equals(alias, StringComparison.OrdinalIgnoreCase));
        if (match.Key != null)
        {
            normalizedReference = match.Value;
            return true;
        }
        normalizedReference = null!;
        return false;
    }

    public IReadOnlyDictionary<string, string> GetAllAliases() => _config.Aliases;

    // IStopWordProvider
    public bool IsStopWord(string word) => _config.StopWords.Contains(word);

    // IRankingWeightsProvider
    public double GetWeight(string factor)
    {
        if (_config.RankingWeights.TryGetValue(factor, out double val)) return val;
        return 0;
    }

    public IReadOnlyDictionary<string, double> GetAllWeights() => _config.RankingWeights;

    private class AliasConfigItem
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;
    }
}
