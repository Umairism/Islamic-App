using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Common.Interfaces;

public interface ITranslationRepository
{
    Task<IEnumerable<TranslationInfoDto>> GetTranslationsAsync(PaginationParams pagination, CancellationToken cancellationToken);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken);
}
