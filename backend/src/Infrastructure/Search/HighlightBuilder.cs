using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IslamicApp.Application.Research.Interfaces;

namespace IslamicApp.Infrastructure.Search;

public class HighlightBuilder : IHighlightBuilder
{
    public List<string> BuildHighlights(string text, List<string> terms)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        if (terms == null || terms.Count == 0)
        {
            return new List<string> { text.Length > 150 ? text.Substring(0, 147) + "..." : text };
        }

        // Filter out very short tokens to avoid redundant highlighting of single characters
        var cleanTerms = terms
            .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 1)
            .Select(Regex.Escape)
            .ToList();

        if (cleanTerms.Count == 0)
        {
            return new List<string> { text.Length > 150 ? text.Substring(0, 147) + "..." : text };
        }

        // Try exact word boundary match first
        string pattern = @"\b(" + string.Join("|", cleanTerms) + @")\b";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        var matches = regex.Matches(text);
        if (matches.Count == 0)
        {
            // Fallback to substring matching for non-separated languages (like Arabic/Urdu) or partial words
            string fallbackPattern = "(" + string.Join("|", cleanTerms) + ")";
            regex = new Regex(fallbackPattern, RegexOptions.IgnoreCase);
            matches = regex.Matches(text);
        }

        if (matches.Count == 0)
        {
            return new List<string> { text.Length > 150 ? text.Substring(0, 147) + "..." : text };
        }

        var highlights = new List<string>();
        // Process up to 2 distinct match windows
        int count = 0;
        foreach (Match match in matches)
        {
            if (count >= 2) break;

            int start = Math.Max(0, match.Index - 60);
            int end = Math.Min(text.Length, match.Index + match.Length + 60);

            string snippet = text.Substring(start, end - start);
            if (start > 0) snippet = "..." + snippet;
            if (end < text.Length) snippet = snippet + "...";

            // Apply HTML tag highlights on matched terms inside snippet
            string highlighted = regex.Replace(snippet, "<em>$1</em>");
            highlights.Add(highlighted);
            count++;
        }

        return highlights;
    }
}
