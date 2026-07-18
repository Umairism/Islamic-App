using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IResearchRenderer
{
    string FormatType { get; }
    Task<RenderResult> RenderAsync(ResearchResult result, CancellationToken cancellationToken);
}
// Interface signature matching asynchronous task rendering
