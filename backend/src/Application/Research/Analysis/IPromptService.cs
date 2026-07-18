using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IPromptService
{
    Task<ResearchPrompt> BuildPromptAsync(ResearchContext context, CancellationToken cancellationToken);
}
