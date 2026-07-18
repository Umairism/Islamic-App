using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface ITextGenerationProvider
{
    string ProviderName { get; }
    
    bool SupportsJsonMode { get; }
    bool SupportsStreaming { get; }
    bool SupportsSeed { get; }
    bool SupportsVision { get; }
    bool SupportsTools { get; }

    Task<GenerationResponse> GenerateAsync(
        ResearchPrompt prompt,
        GenerationOptions options,
        CancellationToken cancellationToken);
}
