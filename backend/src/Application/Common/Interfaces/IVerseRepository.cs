using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Common.Interfaces;

public interface IVerseRepository
{
    Task<VerseDto> GetVerseAsync(int surahNumber, int ayahNumber, IEnumerable<string> languages, CancellationToken cancellationToken);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken);
}
