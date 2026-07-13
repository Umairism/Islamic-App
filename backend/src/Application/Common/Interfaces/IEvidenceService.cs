using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Common.Interfaces;

public interface IEvidenceService
{
    Task<VerseDto> GetEvidenceByReferenceAsync(string reference, IEnumerable<string> languages, CancellationToken cancellationToken);
    Task<IEnumerable<SurahDto>> GetSurahsAsync(PaginationParams pagination, CancellationToken cancellationToken);
    Task<SurahDto> GetSurahByNumberAsync(int number, CancellationToken cancellationToken);
    Task<IEnumerable<TranslationInfoDto>> GetTranslationsAsync(PaginationParams pagination, CancellationToken cancellationToken);
    Task<int> GetTotalSurahCountAsync(CancellationToken cancellationToken);
    Task<int> GetTotalTranslationCountAsync(CancellationToken cancellationToken);
}
