using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;

namespace IslamicApp.Infrastructure.Search;

public class QuranSearcher : ISourceSearcher
{
    private readonly ApplicationDbContext _dbContext;

    public EvidenceSource Source => EvidenceSource.Quran;

    public SearcherCapabilities Capabilities => new(
        new HashSet<SearchLanguage> { SearchLanguage.Arabic, SearchLanguage.English, SearchLanguage.Urdu },
        new HashSet<SearchFeature> { SearchFeature.ExactReference, SearchFeature.PrefixSearch, SearchFeature.FuzzySearch }
    );

    public QuranSearcher(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<EvidenceMatch>> SearchAsync(SearchContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matches = new List<EvidenceMatch>();

        // Load Dataset Metadata from db
        var dataset = await _dbContext.Datasets
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id.StartsWith("quran"), cancellationToken);
        
        string checksum = dataset?.Checksum ?? "default_checksum";
        string version = dataset?.Version ?? "3.1.2";

        // Scenario 1: Reference match
        if (context.ResolvedReference != null && context.ResolvedReference.Identifier.Source == EvidenceSource.Quran)
        {
            var refMatch = Regex.Match(context.ResolvedReference.FormattedReference, @"^(\d+):(\d+)(?:-(\d+))?$");
            if (refMatch.Success)
            {
                int surah = int.Parse(refMatch.Groups[1].Value);
                int startAyah = int.Parse(refMatch.Groups[2].Value);
                int endAyah = refMatch.Groups[3].Success ? int.Parse(refMatch.Groups[3].Value) : startAyah;

                var verses = await _dbContext.QuranVerses
                    .AsNoTracking()
                    .Include(v => v.Surah)
                    .Include(v => v.Translations)
                    .Where(v => v.SurahNumber == surah && v.AyahNumber >= startAyah && v.AyahNumber <= endAyah)
                    .ToListAsync(cancellationToken);

                foreach (var v in verses)
                {
                    matches.Add(MapToMatch(v, checksum, version));
                }
            }
        }
        else if (context.ResolvedReference == null)
        {
            // Scenario 2: Keyword search across expanded tokens
            var targetTokens = context.ExpandedTokens;
            if (targetTokens.Count > 0)
            {
                var matchedIds = new HashSet<string>();

                foreach (var token in targetTokens)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Translations
                    var transIds = await _dbContext.QuranTranslations
                        .Where(t => t.Text.Contains(token))
                        .Select(t => t.VerseId)
                        .Take(50)
                        .ToListAsync(cancellationToken);
                    
                    foreach (var id in transIds) matchedIds.Add(id);

                    // Arabic Cleaned
                    var verseIds = await _dbContext.QuranVerses
                        .Where(v => v.ArabicCleaned.Contains(token))
                        .Select(v => v.Id)
                        .Take(50)
                        .ToListAsync(cancellationToken);

                    foreach (var id in verseIds) matchedIds.Add(id);

                    // Surah English/Transliteration
                    var surahVerseIds = await _dbContext.QuranVerses
                        .Where(v => v.Surah.EnglishName.Contains(token) || v.Surah.Transliteration.Contains(token))
                        .Select(v => v.Id)
                        .Take(30)
                        .ToListAsync(cancellationToken);

                    foreach (var id in surahVerseIds) matchedIds.Add(id);
                }

                // Batch fetch matched verses details
                var matchedVerses = await _dbContext.QuranVerses
                    .AsNoTracking()
                    .Include(v => v.Surah)
                    .Include(v => v.Translations)
                    .Where(v => matchedIds.Contains(v.Id))
                    .ToListAsync(cancellationToken);

                foreach (var v in matchedVerses)
                {
                    matches.Add(MapToMatch(v, checksum, version));
                }
            }
        }

        return matches;
    }

    private static EvidenceMatch MapToMatch(Persistence.Entities.QuranVerseEntity v, string checksum, string version)
    {
        var meta = new EvidenceMetadata(
            Dataset: "Quran-JSON",
            Edition: "Sahih International",
            Translator: "Sahih International",
            Language: "en",
            Version: version,
            Checksum: checksum
        );

        return new EvidenceMatch
        {
            Source = EvidenceSource.Quran,
            Collection = "Quran",
            Reference = $"{v.SurahNumber}:{v.AyahNumber}",
            PrimaryText = v.ArabicCleaned,
            Translations = v.Translations
                .Select(t => new TranslationDto { Language = t.Language, Translator = t.Translator, Text = t.Text })
                .ToList(),
            Metadata = meta
        };
    }
}
