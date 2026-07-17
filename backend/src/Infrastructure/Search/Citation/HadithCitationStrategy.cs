using System;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Citation;

public class HadithCitationStrategy : ICitationStrategy
{
    public EvidenceSource Source => EvidenceSource.Hadith;

    public string Format(ResearchReference reference, string language)
    {
        if (reference == null) return string.Empty;
        if (reference is not HadithReference href) return reference.ToDisplayString();

        string lang = language?.Trim().ToLowerInvariant() ?? "en";
        string collName = href.Collection;

        if (lang.StartsWith("ar"))
        {
            if (collName.Contains("Bukhari", StringComparison.OrdinalIgnoreCase))
                collName = "صحيح البخاري";
            else if (collName.Contains("Muslim", StringComparison.OrdinalIgnoreCase))
                collName = "صحيح مسلم";
            return $"{collName}، كتاب {href.BookNumber}، حديث {href.HadithNumber}";
        }
        else
        {
            return $"{collName} Book {href.BookNumber}, Hadith {href.HadithNumber}";
        }
    }
}
