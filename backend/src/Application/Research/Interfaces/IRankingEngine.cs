using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface IRankingEngine
{
    void Rank(SearchContext context);
}
