using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research;

public class MarkdownRenderer : IResearchRenderer
{
    public string FormatType => "markdown";

    public Task<RenderResult> RenderAsync(ResearchResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sb = new StringBuilder();
        sb.AppendLine($"# Research Synthesis Report: {result.ExecutionContext.Context.Input.Query.Query.Original}");
        sb.AppendLine();
        sb.AppendLine($"**Methodology:** {result.Reasoning.Methodology}");
        sb.AppendLine($"**Prompt Version:** {result.Reasoning.PromptVersion}");
        sb.AppendLine($"**Metadata:** Provider: {result.Reasoning.Metadata.Provider}, Model: {result.Reasoning.Metadata.Model}");
        sb.AppendLine();
        
        sb.AppendLine("## 1. Executive Summary");
        sb.AppendLine(result.Reasoning.Summary);
        sb.AppendLine();

        sb.AppendLine("## 2. Synthesized Claims");
        foreach (var claim in result.Reasoning.Claims)
        {
            sb.AppendLine($"- **{claim.Statement}**");
            sb.AppendLine($"  - *Type:* {claim.ClaimType} | *Origin:* {claim.Origin} | *Confidence:* {claim.Confidence.Value:F2} ({claim.Confidence.Level})");
            sb.AppendLine($"  - *Supporting Evidence:* {string.Join(", ", claim.SupportingEvidence)}");
            sb.AppendLine();
        }

        sb.AppendLine("## 3. Findings");
        foreach (var finding in result.Reasoning.Findings)
        {
            sb.AppendLine($"### {finding.Heading} (Section: {finding.Section})");
            sb.AppendLine(finding.Details);
            sb.AppendLine($"*Cited references:* {string.Join(", ", finding.CitedReferences)}");
            sb.AppendLine();
        }

        sb.AppendLine("## 4. Limitations");
        foreach (var limit in result.Reasoning.Limitations)
        {
            sb.AppendLine($"- **{limit.LimitationDescription}** (Impact: *{limit.Impact}*)");
            sb.AppendLine($"  *Affected Evidences:* {string.Join(", ", limit.AffectedEvidences)}");
            sb.AppendLine();
        }

        var md = sb.ToString();
        var renderResult = new RenderResult(
            Content: md,
            ContentType: "text/markdown",
            Extension: "md",
            FileName: $"research_report_{Guid.NewGuid().ToString()[..8]}.md"
        );

        return Task.FromResult(renderResult);
    }
}
