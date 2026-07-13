using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Common.Interfaces;

public interface IImportSessionRepository
{
    Task<IEnumerable<ImportSessionDto>> GetImportSessionsAsync(PaginationParams pagination, CancellationToken cancellationToken);
    Task<ImportSessionDto> GetLatestImportSessionAsync(CancellationToken cancellationToken);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken);
}
