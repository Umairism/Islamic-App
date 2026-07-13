using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.Infrastructure.Persistence.Repositories;

public class TranslationRepository : ITranslationRepository
{
    private readonly ApplicationDbContext _context;

    public TranslationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TranslationInfoDto>> GetTranslationsAsync(PaginationParams pagination, CancellationToken cancellationToken)
    {
        var list = await _context.QuranTranslations
            .AsNoTracking()
            .Select(t => new { t.Language, t.Translator })
            .Distinct()
            .OrderBy(t => t.Language)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return list.Select(x => new TranslationInfoDto
        {
            Language = x.Language,
            Translator = x.Translator
        }).ToList();
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return await _context.QuranTranslations
            .AsNoTracking()
            .Select(t => new { t.Language, t.Translator })
            .Distinct()
            .CountAsync(cancellationToken);
    }
}
