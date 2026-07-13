using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IslamicApp.Application.Research.Interfaces;

namespace IslamicApp.Infrastructure.Search;

public class Tokenizer : ITokenizer
{
    private readonly Dictionary<string, HashSet<string>> _stopwords = new(StringComparer.OrdinalIgnoreCase);
    public string Checksum { get; private set; } = string.Empty;

    public Tokenizer()
    {
        LoadStopwords();
    }

    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Split by whitespace and common punctuation delimiters
        return text.Split(new[] { ' ', '\t', '\r', '\n', '-', '_', '/', '\\', '.', ',', ';', ':', '?', '!' }, 
            StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();
    }

    public List<string> RemoveStopwords(List<string> tokens, string language)
    {
        if (tokens == null || tokens.Count == 0)
            return new List<string>();

        string langKey = NormalizeLanguageKey(language);

        if (!_stopwords.TryGetValue(langKey, out var stopSet))
        {
            // If specific language is not found, fallback to english or return all
            if (!_stopwords.TryGetValue("en", out stopSet))
            {
                return tokens;
            }
        }

        return tokens.Where(token => !stopSet.Contains(token.ToLowerInvariant())).ToList();
    }

    private string NormalizeLanguageKey(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "en";

        string clean = language.Trim().ToLowerInvariant();
        if (clean.StartsWith("en")) return "en";
        if (clean.StartsWith("ar")) return "ar";
        if (clean.StartsWith("ur")) return "ur";
        return "en";
    }

    private void LoadStopwords()
    {
        try
        {
            string path = FindConfigPath("stopwords.json");
            string content = File.ReadAllText(path);
            
            // Compute checksum
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                Checksum = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }

            var dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(content);
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    _stopwords[kvp.Key] = new HashSet<string>(kvp.Value, StringComparer.OrdinalIgnoreCase);
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback default stop words if file reading fails during tests/etc.
            _stopwords["en"] = new HashSet<string>(new[] { "the", "of", "is", "and", "a", "to", "in", "that", "it" }, StringComparer.OrdinalIgnoreCase);
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
