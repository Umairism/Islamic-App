using System;
using System.Text.Json.Serialization;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

[JsonDerivedType(typeof(QuranReference), typeDiscriminator: "quran")]
[JsonDerivedType(typeof(HadithReference), typeDiscriminator: "hadith")]
public abstract record ResearchReference
{
    public abstract EvidenceSource Source { get; }
    public abstract string LookupKey { get; }
    public abstract string ToDisplayString();
}

public record QuranReference : ResearchReference
{
    public override EvidenceSource Source => EvidenceSource.Quran;
    public int Surah { get; init; }
    public int Ayah { get; init; }

    public QuranReference(int surah, int ayah)
    {
        if (surah < 1 || surah > 114)
            throw new ArgumentOutOfRangeException(nameof(surah), "Surah must be between 1 and 114.");
        if (ayah < 1)
            throw new ArgumentOutOfRangeException(nameof(ayah), "Ayah must be greater than 0.");

        Surah = surah;
        Ayah = ayah;
    }

    public override string LookupKey => $"{Surah}:{Ayah}";
    public override string ToDisplayString() => $"Qur'an {Surah}:{Ayah}";
}

public record HadithReference : ResearchReference
{
    public override EvidenceSource Source => EvidenceSource.Hadith;
    public string Collection { get; init; }
    public int BookNumber { get; init; }
    public int HadithNumber { get; init; }

    public HadithReference(string collection, int bookNumber, int hadithNumber)
    {
        if (string.IsNullOrWhiteSpace(collection))
            throw new ArgumentException("Collection name cannot be empty.", nameof(collection));
        if (bookNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(bookNumber), "Book number must be greater than 0.");
        if (hadithNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(hadithNumber), "Hadith number must be greater than 0.");

        Collection = collection;
        BookNumber = bookNumber;
        HadithNumber = hadithNumber;
    }

    public override string LookupKey => $"{BookNumber}:{HadithNumber}";
    public override string ToDisplayString() => $"{Collection} Book {BookNumber}, Hadith {HadithNumber}";
}
