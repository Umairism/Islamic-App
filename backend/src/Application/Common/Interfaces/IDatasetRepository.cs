using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Common.Interfaces;

public interface IDatasetRepository
{
    Task<IEnumerable<DatasetDto>> GetDatasetsAsync(PaginationParams pagination, CancellationToken cancellationToken);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken);
    Task<bool> PingAsync(CancellationToken cancellationToken);
}
