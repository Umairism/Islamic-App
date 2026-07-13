using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Mappings;

namespace IslamicApp.Infrastructure.Persistence.Repositories;

public class ImportSessionRepository : IImportSessionRepository
{
    private readonly ApplicationDbContext _context;

    public ImportSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ImportSessionDto>> GetImportSessionsAsync(PaginationParams pagination, CancellationToken cancellationToken)
    {
        var entities = await _context.ImportSessions
            .AsNoTracking()
            .OrderByDescending(i => i.StartedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return entities.Select(i => i.ToDto()).ToList();
    }

    public async Task<ImportSessionDto> GetLatestImportSessionAsync(CancellationToken cancellationToken)
    {
        var entity = await _context.ImportSessions
            .AsNoTracking()
            .OrderByDescending(i => i.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToDto();
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return await _context.ImportSessions.CountAsync(cancellationToken);
    }
}
