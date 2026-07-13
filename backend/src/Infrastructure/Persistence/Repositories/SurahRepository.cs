using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Mappings;

namespace IslamicApp.Infrastructure.Persistence.Repositories;

public class SurahRepository : ISurahRepository
{
    private readonly ApplicationDbContext _context;

    public SurahRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SurahDto>> GetSurahsAsync(PaginationParams pagination, CancellationToken cancellationToken)
    {
        var entities = await _context.Surahs
            .AsNoTracking()
            .OrderBy(s => s.Number)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return entities.Select(s => s.ToDto()).ToList();
    }

    public async Task<SurahDto> GetSurahByNumberAsync(int number, CancellationToken cancellationToken)
    {
        var entity = await _context.Surahs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Number == number, cancellationToken);

        return entity?.ToDto();
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return await _context.Surahs.CountAsync(cancellationToken);
    }
}
