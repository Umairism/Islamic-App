using System;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Citation;

public class HadithCitationStrategy : ICitationStrategy
{
    public EvidenceSource Source => EvidenceSource.Hadith;

    public string Format(KnowledgeIdentifier identifier, EvidenceMetadata metadata)
    {
        if (identifier == null) return string.Empty;

        string lang = identifier.Language.Trim().ToLowerInvariant();
        string collName = identifier.Collection; // e.g., "Sahih al-Bukhari" or "Sahih Muslim"

        if (lang.StartsWith("ar"))
        {
            // E.g., صحيح البخاري، كتاب 3، حديث 54
            return $"{collName}، كتاب {identifier.Book}، حديث {identifier.VerseOrHadithNumber}";
        }
        else
        {
            // E.g., Sahih al-Bukhari Book 3, Hadith 54
            return $"{collName} Book {identifier.Book}, Hadith {identifier.VerseOrHadithNumber}";
        }
    }
}
