using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IslamicApp.Application.Research.Interfaces;

namespace IslamicApp.Infrastructure.Search;

public class SynonymEngine : ISynonymEngine
{
    private class SynonymEntry
    {
        public string Term { get; set; } = string.Empty;
        public double Weight { get; set; } = 1.0;
    }

    private readonly Dictionary<string, List<SynonymEntry>> _synonyms = new(StringComparer.OrdinalIgnoreCase);
    public string Checksum { get; private set; } = string.Empty;

    public SynonymEngine()
    {
        LoadSynonyms();
    }

    public List<string> ExpandTokens(List<string> tokens, out Dictionary<string, double> termWeights)
    {
        termWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (tokens == null || tokens.Count == 0)
            return new List<string>();

        var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            expanded.Add(token);
            termWeights[token] = 1.0; // Original query terms have max weight

            if (_synonyms.TryGetValue(token, out var entries))
            {
                foreach (var entry in entries)
                {
                    if (expanded.Add(entry.Term))
                    {
                        termWeights[entry.Term] = entry.Weight;
                    }
                    else
                    {
                        // Keep highest weight if term is encountered multiple times
                        termWeights[entry.Term] = Math.Max(termWeights[entry.Term], entry.Weight);
                    }
                }
            }
        }

        return expanded.ToList();
    }

    private void LoadSynonyms()
    {
        try
        {
            string path = FindConfigPath("synonyms.json");
            string content = File.ReadAllText(path);

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                Checksum = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }

            var dict = JsonSerializer.Deserialize<Dictionary<string, List<SynonymEntry>>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    _synonyms[kvp.Key] = kvp.Value;
                }
            }
        }
        catch
        {
            Checksum = "default_fallback";
        }
    }

    private static string FindConfigPath(string fileName)
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var configDir = Path.Combine(current, "Configuration", "Search");
            if (Directory.Exists(configDir))
            {
                var filePath = Path.Combine(configDir, fileName);
                if (File.Exists(filePath))
                    return filePath;
            }

            var rootConfigDir = Path.Combine(current, "backend", "Configuration", "Search");
            if (Directory.Exists(rootConfigDir))
            {
                var filePath = Path.Combine(rootConfigDir, fileName);
                if (File.Exists(filePath))
                    return filePath;
            }

            var parent = Directory.GetParent(current);
            if (parent == null || parent.FullName == current) break;
            current = parent.FullName;
        }
        throw new FileNotFoundException($"Configuration file {fileName} not found.");
    }
}
