using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Semantic.Query;

namespace IslamicApp.Infrastructure.Semantic.Query;

public class QueryRewriter : IQueryRewriter
{
    private readonly List<ConceptOntology> _ontology = new();

    public QueryRewriter()
    {
        LoadOntology();
    }

    private void LoadOntology()
    {
        var path = FindOntologyPath();
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            // Seed defaults if configuration files are not found (robust testing/fallback)
            _ontology.Add(new ConceptOntology(
                Id: "family.parents",
                DisplayName: "Parents",
                Aliases: new[] { "walidayn", "mother", "father" },
                Roots: new[] { "ولد" },
                Related: new[] { "family.parents.rights", "family.parents.kindness" },
                Broader: new[] { "family.relationships" },
                Narrower: new[] { "family.mother", "family.father" },
                SeeAlso: new[] { "family.relatives" }
            ));
            return;
        }

        try
        {
            var files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var concept = JsonSerializer.Deserialize<ConceptOntology>(content);
                    if (concept != null)
                    {
                        _ontology.Add(concept);
                    }
                }
                catch {}
            }
        }
        catch {}
    }

    private static string FindOntologyPath()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var configDir = Path.Combine(current, "Configuration", "Semantic", "Ontology");
            if (Directory.Exists(configDir)) return configDir;

            var rootConfigDir = Path.Combine(current, "backend", "Configuration", "Semantic", "Ontology");
            if (Directory.Exists(rootConfigDir)) return rootConfigDir;

            var parent = Directory.GetParent(current)?.FullName;
            if (parent == current) break;
            current = parent!;
        }
        return string.Empty;
    }

    public Task<SemanticQuery> RewriteAsync(string query, CancellationToken cancellationToken)
    {
        string raw = query ?? string.Empty;
        var cleanQuery = raw.Trim().ToLowerInvariant();

        var matchedConcepts = new List<string>();
        var expandedTokens = new List<string>();
        var arabicRoots = new List<string>();

        foreach (var concept in _ontology)
        {
            bool isMatch = cleanQuery.Contains(concept.DisplayName.ToLowerInvariant()) ||
                          concept.Aliases.Any(alias => cleanQuery.Contains(alias.ToLowerInvariant())) ||
                          concept.Roots.Any(root => cleanQuery.Contains(root));

            if (isMatch)
            {
                matchedConcepts.Add(concept.Id);
                expandedTokens.AddRange(concept.Aliases);
                arabicRoots.AddRange(concept.Roots);
            }
        }

        double confidence = matchedConcepts.Count > 0 ? 0.9 : 0.5;

        return Task.FromResult(new SemanticQuery(
            RawQuery: raw,
            ExpandedTokens: expandedTokens.Distinct().ToList(),
            Concepts: matchedConcepts.Distinct().ToList(),
            ArabicRoots: arabicRoots.Distinct().ToList(),
            Confidence: confidence
        ));
    }
}
