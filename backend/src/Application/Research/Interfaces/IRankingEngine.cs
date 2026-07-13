using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface IRankingEngine
{
    SearchContext Rank(SearchContext context);
}
