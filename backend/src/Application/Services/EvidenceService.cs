using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Exceptions;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Services;

public class EvidenceService : IEvidenceService
{
    private readonly ISurahRepository _surahRepository;
    private readonly IVerseRepository _verseRepository;
    private readonly ITranslationRepository _translationRepository;

    public EvidenceService(
        ISurahRepository surahRepository,
        IVerseRepository verseRepository,
        ITranslationRepository translationRepository)
    {
        _surahRepository = surahRepository;
        _verseRepository = verseRepository;
        _translationRepository = translationRepository;
    }

    public async Task<VerseDto> GetEvidenceByReferenceAsync(string reference, IEnumerable<string> languages, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ValidationException("reference", "Reference cannot be empty.");
        }

        var parts = reference.Split(':');
        if (parts.Length != 2)
        {
            throw new ValidationException("reference", "Reference must be in format 'surah:ayah' (e.g. '2:255').");
        }

        if (!int.TryParse(parts[0], out int surahNumber) || surahNumber < 1 || surahNumber > 114)
        {
            throw new ValidationException("surah", "Surah number must be an integer between 1 and 114.");
        }

        var surah = await _surahRepository.GetSurahByNumberAsync(surahNumber, cancellationToken);
        if (surah == null)
        {
            throw new NotFoundException($"Surah with number {surahNumber} was not found.");
        }

        if (!int.TryParse(parts[1], out int ayahNumber) || ayahNumber < 1 || ayahNumber > surah.TotalVerses)
        {
            throw new ValidationException("ayah", $"Ayah number must be an integer between 1 and {surah.TotalVerses} for Surah {surah.Transliteration}.");
        }

        var verse = await _verseRepository.GetVerseAsync(surahNumber, ayahNumber, languages, cancellationToken);
        if (verse == null)
        {
            throw new NotFoundException($"Verse {reference} was not found in database.");
        }

        return verse;
    }

    public async Task<IEnumerable<SurahDto>> GetSurahsAsync(PaginationParams pagination, CancellationToken cancellationToken)
    {
        if (pagination == null)
        {
            pagination = new PaginationParams();
        }

        return await _surahRepository.GetSurahsAsync(pagination, cancellationToken);
    }

    public async Task<SurahDto> GetSurahByNumberAsync(int number, CancellationToken cancellationToken)
    {
        if (number < 1 || number > 114)
        {
            throw new NotFoundException($"Surah with number {number} not found.");
        }

        var surah = await _surahRepository.GetSurahByNumberAsync(number, cancellationToken);
        if (surah == null)
        {
            throw new NotFoundException($"Surah {number} not found.");
        }

        return surah;
    }

    public async Task<IEnumerable<TranslationInfoDto>> GetTranslationsAsync(PaginationParams pagination, CancellationToken cancellationToken)
    {
        if (pagination == null)
        {
            pagination = new PaginationParams();
        }

        return await _translationRepository.GetTranslationsAsync(pagination, cancellationToken);
    }

    public async Task<int> GetTotalSurahCountAsync(CancellationToken cancellationToken)
    {
        return await _surahRepository.GetTotalCountAsync(cancellationToken);
    }

    public async Task<int> GetTotalTranslationCountAsync(CancellationToken cancellationToken)
    {
        return await _translationRepository.GetTotalCountAsync(cancellationToken);
    }
}
