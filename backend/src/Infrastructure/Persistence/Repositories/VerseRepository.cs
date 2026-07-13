using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Mappings;

namespace IslamicApp.Infrastructure.Persistence.Repositories;

public class VerseRepository : IVerseRepository
{
    private readonly ApplicationDbContext _context;

    public VerseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VerseDto> GetVerseAsync(int surahNumber, int ayahNumber, IEnumerable<string> languages, CancellationToken cancellationToken)
    {
        var entity = await _context.QuranVerses
            .AsNoTracking()
            .Include(v => v.Translations)
            .FirstOrDefaultAsync(v => v.SurahNumber == surahNumber && v.AyahNumber == ayahNumber, cancellationToken);

        return entity?.ToDto(languages);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return await _context.QuranVerses.CountAsync(cancellationToken);
    }
}
