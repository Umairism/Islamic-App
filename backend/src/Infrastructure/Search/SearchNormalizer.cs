using System;
using System.Text;
using System.Text.RegularExpressions;
using IslamicApp.Application.Research.Interfaces;

namespace IslamicApp.Infrastructure.Search;

public class SearchNormalizer : ISearchNormalizer
{
    private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);
    
    // Punctuation to strip/replace
    private static readonly Regex PunctuationRegex = new(@"[.,\/#!$%\^&\*;:{}=\-_`~()""'?|]", RegexOptions.Compiled);
    private static readonly Regex ArabicPunctuationRegex = new(@"[؟،؛«»]", RegexOptions.Compiled);

    public string Normalize(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        // Lowercase
        string normalized = query.ToLowerInvariant().Trim();

        // Remove tatweel (kashida)
        normalized = normalized.Replace("\u0640", "");

        // Remove Arabic diacritics (harakat)
        normalized = RemoveArabicDiacritics(normalized);

        // Normalize Alif variants (أ , إ , آ , ٱ -> ا)
        normalized = NormalizeAlif(normalized);

        // Clean punctuation
        normalized = PunctuationRegex.Replace(normalized, " ");
        normalized = ArabicPunctuationRegex.Replace(normalized, " ");

        // Standardize spacing
        normalized = MultipleSpacesRegex.Replace(normalized, " ").Trim();

        return normalized;
    }

    private static string RemoveArabicDiacritics(string text)
    {
        var sb = new StringBuilder();
        foreach (char c in text)
        {
            // Range for common Arabic diacritics: \u064b to \u0652, plus superscript alif \u0670
            if (c >= '\u064b' && c <= '\u0652')
                continue;
            if (c == '\u0670')
                continue;

            sb.Append(c);
        }
        return sb.ToString();
    }

    private static string NormalizeAlif(string text)
    {
        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (c == 'أ' || c == 'إ' || c == 'آ' || c == 'ٱ' || c == '\u0671')
            {
                sb.Append('ا');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
