using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Common.Interfaces;

public interface ISurahRepository
{
    Task<IEnumerable<SurahDto>> GetSurahsAsync(PaginationParams pagination, CancellationToken cancellationToken);
    Task<SurahDto> GetSurahByNumberAsync(int number, CancellationToken cancellationToken);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken);
}
