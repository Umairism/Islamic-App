using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface IQueryAnalyzer
{
    Task<QueryAnalysis> AnalyzeAsync(SearchRequest request);
}
