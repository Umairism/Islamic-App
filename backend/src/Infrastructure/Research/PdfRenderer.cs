using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research;

public class PdfRenderer : IResearchRenderer
{
    public string FormatType => "pdf";

    public Task<RenderResult> RenderAsync(ResearchResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // PDF renderer placeholder returning simulated binary PDF content block
        var mockPdfContent = $"%PDF-1.4\n%mock pdf report content for: {result.ExecutionContext.Context.Input.Query.Query.Original}\n%%EOF";
        
        var renderResult = new RenderResult(
            Content: mockPdfContent,
            ContentType: "application/pdf",
            Extension: "pdf",
            FileName: $"research_report_{Guid.NewGuid().ToString()[..8]}.pdf"
        );

        return Task.FromResult(renderResult);
    }
}
