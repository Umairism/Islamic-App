using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Mappings;

namespace IslamicApp.Infrastructure.Persistence.Repositories;

public class DatasetRepository : IDatasetRepository
{
    private readonly ApplicationDbContext _context;

    public DatasetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DatasetDto>> GetDatasetsAsync(PaginationParams pagination, CancellationToken cancellationToken)
    {
        var entities = await _context.Datasets
            .AsNoTracking()
            .OrderByDescending(d => d.ImportedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return entities.Select(d => d.ToDto()).ToList();
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return await _context.Datasets.CountAsync(cancellationToken);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}
