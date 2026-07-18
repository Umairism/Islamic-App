using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research;

public class HtmlRenderer : IResearchRenderer
{
    public string FormatType => "html";

    public Task<RenderResult> RenderAsync(ResearchResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head><title>Research Synthesis Report</title></head>");
        sb.AppendLine("<body style=\"font-family: Arial, sans-serif; margin: 20px; line-height: 1.6;\">");
        sb.AppendLine($"<h1>Research Synthesis Report: {result.ExecutionContext.Context.Input.Query.Query.Original}</h1>");
        sb.AppendLine($"<p><strong>Methodology:</strong> {result.Reasoning.Methodology} | <strong>Prompt Version:</strong> {result.Reasoning.PromptVersion}</p>");
        sb.AppendLine($"<p><strong>AI Provider:</strong> {result.Reasoning.Metadata.Provider} ({result.Reasoning.Metadata.Model})</p>");
        sb.AppendLine("<hr />");
        
        sb.AppendLine("<h2>1. Executive Summary</h2>");
        sb.AppendLine($"<p>{result.Reasoning.Summary}</p>");

        sb.AppendLine("<h2>2. Synthesized Claims</h2>");
        sb.AppendLine("<ul>");
        foreach (var claim in result.Reasoning.Claims)
        {
            sb.AppendLine("<li>");
            sb.AppendLine($"<strong>{claim.Statement}</strong><br />");
            sb.AppendLine($"<em>Type:</em> {claim.ClaimType} | <em>Origin:</em> {claim.Origin} | <em>Confidence:</em> {claim.Confidence.Value:F2} ({claim.Confidence.Level})<br />");
            sb.AppendLine($"<em>Supporting Evidence:</em> {string.Join(", ", claim.SupportingEvidence)}");
            sb.AppendLine("</li>");
        }
        sb.AppendLine("</ul>");

        sb.AppendLine("<h2>3. Findings</h2>");
        foreach (var finding in result.Reasoning.Findings)
        {
            sb.AppendLine($"<h3>{finding.Heading} (Section: {finding.Section})</h3>");
            sb.AppendLine($"<p>{finding.Details}</p>");
            sb.AppendLine($"<p><em>Cited references:</em> {string.Join(", ", finding.CitedReferences)}</p>");
        }

        sb.AppendLine("<h2>4. Limitations</h2>");
        sb.AppendLine("<ul>");
        foreach (var limit in result.Reasoning.Limitations)
        {
            sb.AppendLine("<li>");
            sb.AppendLine($"<strong>{limit.LimitationDescription}</strong> (Impact: <em>{limit.Impact}</em>)<br />");
            sb.AppendLine($"<em>Affected Evidences:</em> {string.Join(", ", limit.AffectedEvidences)}");
            sb.AppendLine("</li>");
        }
        sb.AppendLine("</ul>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var renderResult = new RenderResult(
            Content: sb.ToString(),
            ContentType: "text/html",
            Extension: "html",
            FileName: $"research_report_{Guid.NewGuid().ToString()[..8]}.html"
        );

        return Task.FromResult(renderResult);
    }
}
