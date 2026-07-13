using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IslamicApp.Application.Research.Interfaces;

namespace IslamicApp.Infrastructure.Search;

public class RankingConfiguration : IRankingConfiguration
{
    public double Reference { get; private set; } = 100;
    public double Alias { get; private set; } = 95;
    public double Arabic { get; private set; } = 90;
    public double Translation { get; private set; } = 80;
    public double SurahName { get; private set; } = 75;
    public double Synonym { get; private set; } = 65;
    public double Partial { get; private set; } = 40;
    public string Checksum { get; private set; } = string.Empty;

    public RankingConfiguration()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            string path = FindConfigPath("ranking.json");
            string content = File.ReadAllText(path);

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                Checksum = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }

            var dict = JsonSerializer.Deserialize<Dictionary<string, double>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (dict != null)
            {
                if (dict.TryGetValue("Reference", out double r)) Reference = r;
                if (dict.TryGetValue("Alias", out double a)) Alias = a;
                if (dict.TryGetValue("Arabic", out double ar)) Arabic = ar;
                if (dict.TryGetValue("Translation", out double t)) Translation = t;
                if (dict.TryGetValue("SurahName", out double sn)) SurahName = sn;
                if (dict.TryGetValue("Synonym", out double syn)) Synonym = syn;
                if (dict.TryGetValue("Partial", out double p)) Partial = p;
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
