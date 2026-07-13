using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IslamicApp.Application.Common.Exceptions;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Services;

namespace IslamicApp.UnitTests;

public class EvidenceServiceTests
{
    private readonly MockSurahRepository _surahRepo;
    private readonly MockVerseRepository _verseRepo;
    private readonly MockTranslationRepository _translationRepo;
    private readonly EvidenceService _service;

    public EvidenceServiceTests()
    {
        _surahRepo = new MockSurahRepository();
        _verseRepo = new MockVerseRepository();
        _translationRepo = new MockTranslationRepository();
        _service = new EvidenceService(_surahRepo, _verseRepo, _translationRepo);
    }

    [Fact]
    public async Task GetEvidenceByReference_ValidReference_ReturnsVerseDto()
    {
        // Arrange
        _surahRepo.Surahs.Add(new SurahDto { Number = 2, Transliteration = "Al-Baqarah", TotalVerses = 286 });
        _verseRepo.Verses[(2, 255)] = new VerseDto
        {
            SurahNumber = 2,
            AyahNumber = 255,
            ArabicText = "Allahu la ilaha...",
            ArabicCleaned = "Allahu la ilaha..."
        };

        // Act
        var result = await _service.GetEvidenceByReferenceAsync("2:255", null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SurahNumber);
        Assert.Equal(255, result.AyahNumber);
    }

    [Fact]
    public async Task GetEvidenceByReference_InvalidReferenceFormat_ThrowsValidationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetEvidenceByReferenceAsync("invalid_reference", null, CancellationToken.None));
        
        Assert.Contains("Reference must be in format", ex.Errors["reference"].First());
    }

    [Fact]
    public async Task GetEvidenceByReference_InvalidSurahNumber_ThrowsValidationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetEvidenceByReferenceAsync("150:1", null, CancellationToken.None));
        
        Assert.Contains("Surah number must be an integer between 1 and 114", ex.Errors["surah"].First());
    }

    [Fact]
    public async Task GetEvidenceByReference_InvalidAyahNumber_ThrowsValidationException()
    {
        // Arrange
        _surahRepo.Surahs.Add(new SurahDto { Number = 1, Transliteration = "Al-Fatihah", TotalVerses = 7 });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetEvidenceByReferenceAsync("1:10", null, CancellationToken.None));
        
        Assert.Contains("Ayah number must be an integer between 1 and 7", ex.Errors["ayah"].First());
    }

    [Fact]
    public async Task GetSurahByNumber_ValidNumber_ReturnsSurahDto()
    {
        // Arrange
        _surahRepo.Surahs.Add(new SurahDto { Number = 1, Transliteration = "Al-Fatihah" });

        // Act
        var result = await _service.GetSurahByNumberAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Al-Fatihah", result.Transliteration);
    }

    [Fact]
    public async Task GetSurahByNumber_InvalidNumber_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetSurahByNumberAsync(150, CancellationToken.None));
    }
}

#region Manual Mocks

public class MockSurahRepository : ISurahRepository
{
    public List<SurahDto> Surahs { get; } = new List<SurahDto>();

    public Task<IEnumerable<SurahDto>> GetSurahsAsync(PaginationParams pagination, CancellationToken cancellationToken)
    {
        return Task.FromResult(Surahs.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize));
    }

    public Task<SurahDto> GetSurahByNumberAsync(int number, CancellationToken cancellationToken)
    {
        return Task.FromResult(Surahs.FirstOrDefault(s => s.Number == number));
    }

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Surahs.Count);
    }
}

public class MockVerseRepository : IVerseRepository
{
    public Dictionary<(int Surah, int Ayah), VerseDto> Verses { get; } = new Dictionary<(int, int), VerseDto>();

    public Task<VerseDto> GetVerseAsync(int surahNumber, int ayahNumber, IEnumerable<string> languages, CancellationToken cancellationToken)
    {
        if (Verses.TryGetValue((surahNumber, ayahNumber), out var verse))
        {
            return Task.FromResult(verse);
        }
        return Task.FromResult<VerseDto>(null);
    }

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Verses.Count);
    }
}

public class MockTranslationRepository : ITranslationRepository
{
    public List<TranslationInfoDto> Translations { get; } = new List<TranslationInfoDto>();

    public Task<IEnumerable<TranslationInfoDto>> GetTranslationsAsync(PaginationParams pagination, CancellationToken cancellationToken)
    {
        return Task.FromResult(Translations.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize));
    }

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Translations.Count);
    }
}

#endregion
