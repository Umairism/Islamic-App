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

public class HadithSearcher : ISourceSearcher
{
    private readonly ApplicationDbContext _dbContext;

    public EvidenceSource Source => EvidenceSource.Hadith;

    public SearcherCapabilities Capabilities => new(
        new HashSet<SearchLanguage> { SearchLanguage.Arabic, SearchLanguage.English },
        new HashSet<SearchFeature> { SearchFeature.ExactReference, SearchFeature.PrefixSearch, SearchFeature.NarratorSearch, SearchFeature.FuzzySearch }
    );

    public HadithSearcher(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<EvidenceMatch>> SearchAsync(SearchContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matches = new List<EvidenceMatch>();

        // Scenario 1: Reference lookup
        if (context.ResolvedReference != null && context.ResolvedReference.Identifier.Source == EvidenceSource.Hadith)
        {
            var ident = context.ResolvedReference.Identifier;
            string coll = ident.Collection; // e.g. "Bukhari" or "Muslim"
            
            if (int.TryParse(ident.VerseOrHadithNumber, out int hadithNum))
            {
                var hadiths = await _dbContext.Hadiths
                    .AsNoTracking()
                    .Include(h => h.Collection)
                    .Include(h => h.Book)
                    .Where(h => h.Collection.ShortName.Contains(coll) && h.HadithNumber == hadithNum)
                    .ToListAsync(cancellationToken);

                foreach (var h in hadiths)
                {
                    matches.Add(MapToMatch(h));
                }
            }
        }
        else if (context.ResolvedReference == null)
        {
            // Scenario 2: Keyword search across expanded tokens
            var targetTokens = context.ExpandedTokensList;
            if (targetTokens.Count > 0)
            {
                var matchedIds = new HashSet<string>();

                foreach (var token in targetTokens)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var hadithIds = await _dbContext.Hadiths
                        .Where(h => h.ArabicCleaned.Contains(token) || 
                                    h.EnglishText.Contains(token) || 
                                    h.EnglishNarrator.Contains(token) ||
                                    h.Book.TitleEnglish.Contains(token) ||
                                    h.Chapter.TitleEnglish.Contains(token))
                        .Select(h => h.Id)
                        .Take(50)
                        .ToListAsync(cancellationToken);
                    
                    foreach (var id in hadithIds) matchedIds.Add(id);
                }

                // Batch fetch matched hadiths details
                var matchedHadiths = await _dbContext.Hadiths
                    .AsNoTracking()
                    .Include(h => h.Collection)
                    .Include(h => h.Book)
                    .Where(h => matchedIds.Contains(h.Id))
                    .ToListAsync(cancellationToken);

                foreach (var h in matchedHadiths)
                {
                    matches.Add(MapToMatch(h));
                }
            }
        }

        return matches;
    }

    private static EvidenceMatch MapToMatch(Persistence.Entities.HadithEntity h)
    {
        var meta = new EvidenceMetadata(
            Dataset: h.Collection.DisplayName,
            Edition: "Darussalam",
            Translator: "Muhsin Khan",
            Language: "en",
            Version: "2025.01",
            Checksum: "05eaf3cb5de2fc9454a16ffc991a95b838089670719e39af03bdc3fe074093e7"
        );

        return new EvidenceMatch
        {
            Source = EvidenceSource.Hadith,
            Collection = h.Collection.DisplayName,
            Reference = $"{h.Book.BookNumber}:{h.HadithNumber}",
            PrimaryText = h.ArabicCleaned,
            Translations = new List<TranslationDto>
            {
                new() { Language = "en", Translator = "Muhsin Khan", Text = $"{h.EnglishNarrator} {h.EnglishText}" }
            },
            Metadata = meta
        };
    }
}
