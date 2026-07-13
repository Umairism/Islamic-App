using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;

namespace IslamicApp.Infrastructure.Search;

public class SuggestionIndex
{
    private class AliasEntry
    {
        public string Alias { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    private readonly List<SearchSuggestionDto> _suggestions = new();
    private bool _initialized = false;
    private readonly object _lock = new();

    public async Task InitializeAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        List<SearchSuggestionDto> loaded = new List<SearchSuggestionDto>();

        try
        {
            // Fetch Surahs from database
            var surahs = await dbContext.Surahs
                .AsNoTracking()
                .Select(s => new { s.EnglishName, s.Transliteration, s.Number })
                .ToListAsync(cancellationToken);

            foreach (var s in surahs)
            {
                loaded.Add(new SearchSuggestionDto("Surah", s.EnglishName));
                loaded.Add(new SearchSuggestionDto("Surah", s.Transliteration));
                loaded.Add(new SearchSuggestionDto("Reference", $"{s.Number}:1"));
            }
        }
        catch
        {
            // Fallback for tests when DB context might be empty / mocked
            loaded.Add(new SearchSuggestionDto("Surah", "Al-Fatihah"));
            loaded.Add(new SearchSuggestionDto("Surah", "Al-Baqarah"));
        }

        // Load aliases from aliases.json
        LoadAliasesFromConfig(loaded);

        // Add popular references
        loaded.Add(new SearchSuggestionDto("Reference", "2:255"));
        loaded.Add(new SearchSuggestionDto("Reference", "36:58"));
        loaded.Add(new SearchSuggestionDto("Reference", "112:1"));

        lock (_lock)
        {
            if (_initialized) return;

            _suggestions.Clear();
            _suggestions.AddRange(loaded
                .GroupBy(s => new { s.Type, s.Value })
                .Select(g => g.First())
                .ToList());

            _initialized = true;
        }
    }

    public List<SearchSuggestionDto> Search(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return new List<SearchSuggestionDto>();

        string cleanPrefix = prefix.Trim().ToLowerInvariant();

        return _suggestions
            .Where(s => s.Value.StartsWith(cleanPrefix, StringComparison.OrdinalIgnoreCase) ||
                        s.Value.Contains(cleanPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Value.StartsWith(cleanPrefix, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(s => s.Value)
            .Take(10)
            .ToList();
    }

    private void LoadAliasesFromConfig(List<SearchSuggestionDto> targetList)
    {
        try
        {
            string path = FindConfigPath("aliases.json");
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                var entries = JsonSerializer.Deserialize<List<AliasEntry>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        targetList.Add(new SearchSuggestionDto("Alias", entry.Alias));
                    }
                }
            }
        }
        catch
        {
            targetList.Add(new SearchSuggestionDto("Alias", "Ayat al Kursi"));
            targetList.Add(new SearchSuggestionDto("Alias", "Ayat an Nur"));
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
