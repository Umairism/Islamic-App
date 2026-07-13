using System;
using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Citation;

public class QuranCitationStrategy : ICitationStrategy
{
    private static readonly Dictionary<int, string> _arabicSurahNames = new()
    {
        { 1, "الفاتحة" },
        { 2, "البقرة" },
        { 3, "آل عمران" },
        { 4, "النساء" },
        { 5, "المائدة" }
    };

    public EvidenceSource Source => EvidenceSource.Quran;

    public string Format(KnowledgeIdentifier identifier, EvidenceMetadata metadata)
    {
        if (identifier == null) return string.Empty;

        string lang = identifier.Language.Trim().ToLowerInvariant();

        if (lang.StartsWith("ar"))
        {
            string name = identifier.Collection;
            if (int.TryParse(identifier.Book, out int num) && _arabicSurahNames.TryGetValue(num, out var sName))
            {
                name = sName;
            }
            return $"سورة {name} آية {identifier.VerseOrHadithNumber}";
        }
        else if (lang.StartsWith("ur"))
        {
            string name = identifier.Collection;
            if (int.TryParse(identifier.Book, out int num) && _arabicSurahNames.TryGetValue(num, out var sName))
            {
                name = sName;
            }
            return $"سورۃ {name} آیت {identifier.VerseOrHadithNumber}";
        }
        else
        {
            // English / default format
            return $"Qur'an {identifier.Book}:{identifier.VerseOrHadithNumber}";
        }
    }
}
