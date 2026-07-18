using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.AI.Providers;

public class OpenAIProvider : ITextGenerationProvider
{
    public string ProviderName => "OpenAI";
    public bool SupportsJsonMode => true;
    public bool SupportsStreaming => true;
    public bool SupportsSeed => true;
    public bool SupportsVision => true;
    public bool SupportsTools => true;

    public Task<GenerationResponse> GenerateAsync(
        ResearchPrompt prompt,
        GenerationOptions options,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("OpenAIProvider integration is configured but not active. Use MockProvider for local runs.");
    }
}
