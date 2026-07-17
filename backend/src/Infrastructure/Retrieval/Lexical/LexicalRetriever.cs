using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Lexical;
using IslamicApp.Infrastructure.Persistence;

namespace IslamicApp.Infrastructure.Retrieval.Lexical;

public class LexicalRetriever : ILexicalRetriever
{
    private readonly ApplicationDbContext _dbContext;

    public LexicalRetriever(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> QueryLexicalDocumentsAsync(
        LexicalSearchParameters parameters,
        CancellationToken cancellationToken)
    {
        var docs = new List<KnowledgeDocument>();
        var tokens = parameters.Query.Tokens;

        if (tokens == null || tokens.Count == 0)
            return docs;

        if (parameters.TargetSources.Contains(EvidenceSource.Quran))
        {
            var verseQuery = _dbContext.QuranVerses
                .AsNoTracking()
                .Include(v => v.Translations)
                .AsQueryable();

            var matchedVerses = new List<Persistence.Entities.QuranVerseEntity>();
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var list = await verseQuery
                    .Where(v => v.ArabicCleaned.Contains(token) || v.Translations.Any(t => t.Text.Contains(token)))
                    .Take(parameters.Pagination.PageSize)
                    .ToListAsync(cancellationToken);
                matchedVerses.AddRange(list);
            }

            foreach (var v in matchedVerses.GroupBy(x => x.Id).Select(g => g.First()))
            {
                var refObj = new QuranReference(v.SurahNumber, v.AyahNumber);
                docs.Add(new KnowledgeDocument(
                    Id: $"quran-{v.SurahNumber}-{v.AyahNumber}",
                    Source: EvidenceSource.Quran,
                    Collection: "Quran",
                    Reference: refObj,
                    PrimaryText: v.ArabicCleaned,
                    Translations: v.Translations.Select(t => new TranslationDto { Language = t.Language, Translator = t.Translator, Text = t.Text }).ToList(),
                    DatasetId: "quran-json",
                    ImportSessionId: "default-session"
                ));
            }
        }

        if (parameters.TargetSources.Contains(EvidenceSource.Hadith))
        {
            var hadithQuery = _dbContext.Hadiths
                .AsNoTracking()
                .Include(h => h.Collection)
                .Include(h => h.Book)
                .AsQueryable();

            var matchedHadiths = new List<Persistence.Entities.HadithEntity>();
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var list = await hadithQuery
                    .Where(h => h.ArabicCleaned.Contains(token) || h.EnglishText.Contains(token) || h.EnglishNarrator.Contains(token))
                    .Take(parameters.Pagination.PageSize)
                    .ToListAsync(cancellationToken);
                matchedHadiths.AddRange(list);
            }

            foreach (var h in matchedHadiths.GroupBy(x => x.Id).Select(g => g.First()))
            {
                var refObj = new HadithReference(h.Collection.ShortName, h.Book.BookNumber, h.HadithNumber);
                docs.Add(new KnowledgeDocument(
                    Id: $"hadith-{h.Collection.ShortName}-{h.HadithNumber}",
                    Source: EvidenceSource.Hadith,
                    Collection: h.Collection.DisplayName,
                    Reference: refObj,
                    PrimaryText: h.ArabicCleaned,
                    Translations: new List<TranslationDto>
                    {
                        new() { Language = "en", Translator = "Muhsin Khan", Text = $"{h.EnglishNarrator} {h.EnglishText}" }
                    },
                    DatasetId: $"hadith-{h.Collection.Slug}",
                    ImportSessionId: "default-session"
                ));
            }
        }

        return docs.Take(parameters.Pagination.PageSize * 2).ToList();
    }

    public async Task<KnowledgeDocument?> GetDocumentByReferenceAsync(
        ResearchReference reference,
        CancellationToken cancellationToken)
    {
        if (reference == null) return null;

        if (reference is QuranReference qref)
        {
            var v = await _dbContext.QuranVerses
                .AsNoTracking()
                .Include(v => v.Translations)
                .FirstOrDefaultAsync(v => v.SurahNumber == qref.Surah && v.AyahNumber == qref.Ayah, cancellationToken);

            if (v != null)
            {
                return new KnowledgeDocument(
                    Id: $"quran-{v.SurahNumber}-{v.AyahNumber}",
                    Source: EvidenceSource.Quran,
                    Collection: "Quran",
                    Reference: qref,
                    PrimaryText: v.ArabicCleaned,
                    Translations: v.Translations.Select(t => new TranslationDto { Language = t.Language, Translator = t.Translator, Text = t.Text }).ToList(),
                    DatasetId: "quran-json",
                    ImportSessionId: "default-session"
                );
            }
        }
        else if (reference is HadithReference href)
        {
            var h = await _dbContext.Hadiths
                .AsNoTracking()
                .Include(h => h.Collection)
                .Include(h => h.Book)
                .FirstOrDefaultAsync(h => h.Collection.ShortName.Contains(href.Collection) && h.HadithNumber == href.HadithNumber, cancellationToken);

            if (h != null)
            {
                return new KnowledgeDocument(
                    Id: $"hadith-{h.Collection.ShortName}-{h.HadithNumber}",
                    Source: EvidenceSource.Hadith,
                    Collection: h.Collection.DisplayName,
                    Reference: href,
                    PrimaryText: h.ArabicCleaned,
                    Translations: new List<TranslationDto>
                    {
                        new() { Language = "en", Translator = "Muhsin Khan", Text = $"{h.EnglishNarrator} {h.EnglishText}" }
                    },
                    DatasetId: $"hadith-{h.Collection.Slug}",
                    ImportSessionId: "default-session"
                );
            }
        }

        return null;
    }
}
