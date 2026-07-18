using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research;

public class JsonRenderer : IResearchRenderer
{
    public string FormatType => "json";

    public Task<RenderResult> RenderAsync(ResearchResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Serialize structured reasoning result
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(new
        {
            Query = result.ExecutionContext.Context.Input.Query.Query.Original,
            Reasoning = result.Reasoning,
            Validation = result.Validation,
            Explainability = result.Explainability
        }, options);

        var renderResult = new RenderResult(
            Content: json,
            ContentType: "application/json",
            Extension: "json",
            FileName: $"research_report_{Guid.NewGuid().ToString()[..8]}.json"
        );

        return Task.FromResult(renderResult);
    }
}
