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

    public string Format(ResearchReference reference, string language)
    {
        if (reference == null) return string.Empty;
        if (reference is not QuranReference qref) return reference.ToDisplayString();

        string lang = language?.Trim().ToLowerInvariant() ?? "en";

        if (lang.StartsWith("ar"))
        {
            string name = _arabicSurahNames.TryGetValue(qref.Surah, out var sName) ? sName : qref.Surah.ToString();
            return $"سورة {name} آية {qref.Ayah}";
        }
        else if (lang.StartsWith("ur"))
        {
            string name = _arabicSurahNames.TryGetValue(qref.Surah, out var sName) ? sName : qref.Surah.ToString();
            return $"سورۃ {name} آیت {qref.Ayah}";
        }
        else
        {
            return $"Qur'an {qref.Surah}:{qref.Ayah}";
        }
    }
}
