using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Export;

public class MarkdownWorkspaceWriter : IExportWriter
{
    public string Format => "Markdown";

    public Task<RenderResult> WriteWorkspaceAsync(Workspace workspace, IEnumerable<ResearchDocument> documents, IEnumerable<ResearchNote> notes, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Workspace Export: {workspace.Name}");
        sb.AppendLine($"*Description: {workspace.Description}*");
        sb.AppendLine($"*Exported At: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss zzz}*");
        sb.AppendLine();

        sb.AppendLine("## 1. Research Documents");
        foreach (var doc in documents)
        {
            sb.AppendLine($"### Document: {doc.Title}");
            sb.AppendLine($"- Created: {doc.CreatedAt}");
            sb.AppendLine();
        }

        sb.AppendLine("## 2. Workspace Notes");
        foreach (var note in notes)
        {
            sb.AppendLine($"### Note: {note.Title}");
            sb.AppendLine($"- Created: {note.CreatedAt}");
            sb.AppendLine($"- Updated: {note.UpdatedAt}");
            sb.AppendLine();
            sb.AppendLine(note.Markdown);
            sb.AppendLine();
        }

        var result = new RenderResult(
            Content: sb.ToString(),
            ContentType: "text/markdown",
            Extension: ".md",
            FileName: $"{workspace.Name.ToLower().Replace(" ", "_")}_export.md"
        );

        return Task.FromResult(result);
    }
}

public class HtmlWorkspaceWriter : IExportWriter
{
    public string Format => "HTML";

    public Task<RenderResult> WriteWorkspaceAsync(Workspace workspace, IEnumerable<ResearchDocument> documents, IEnumerable<ResearchNote> notes, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine($"<title>{workspace.Name} - Export</title>");
        sb.AppendLine("<style>body { font-family: sans-serif; line-height: 1.6; margin: 40px; } h1, h2, h3 { color: #2c3e50; }</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>Workspace Export: {workspace.Name}</h1>");
        sb.AppendLine($"<p><em>Description: {workspace.Description}</em></p>");
        sb.AppendLine("<hr />");

        sb.AppendLine("<h2>1. Research Documents</h2>");
        foreach (var doc in documents)
        {
            sb.AppendLine($"<h3>Document: {doc.Title}</h3>");
            sb.AppendLine($"<p>Created: {doc.CreatedAt}</p>");
        }

        sb.AppendLine("<h2>2. Workspace Notes</h2>");
        foreach (var note in notes)
        {
            sb.AppendLine($"<h3>Note: {note.Title}</h3>");
            sb.AppendLine($"<p>Created: {note.CreatedAt}</p>");
            sb.AppendLine($"<div>{note.Markdown}</div>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var result = new RenderResult(
            Content: sb.ToString(),
            ContentType: "text/html",
            Extension: ".html",
            FileName: $"{workspace.Name.ToLower().Replace(" ", "_")}_export.html"
        );

        return Task.FromResult(result);
    }
}

public class PdfWorkspaceWriter : IExportWriter
{
    public string Format => "PDF";

    public async Task<RenderResult> WriteWorkspaceAsync(Workspace workspace, IEnumerable<ResearchDocument> documents, IEnumerable<ResearchNote> notes, CancellationToken cancellationToken)
    {
        // Simple mock PDF rendering wrapper (HTML envelope representation for mock environments)
        var htmlWriter = new HtmlWorkspaceWriter();
        var htmlResult = await htmlWriter.WriteWorkspaceAsync(workspace, documents, notes, cancellationToken);

        return new RenderResult(
            Content: htmlResult.Content,
            ContentType: "application/pdf",
            Extension: ".pdf",
            FileName: $"{workspace.Name.ToLower().Replace(" ", "_")}_export.pdf"
        );
    }
}

public class JsonWorkspaceWriter : IExportWriter
{
    public string Format => "JSON";

    public Task<RenderResult> WriteWorkspaceAsync(Workspace workspace, IEnumerable<ResearchDocument> documents, IEnumerable<ResearchNote> notes, CancellationToken cancellationToken)
    {
        var data = new
        {
            Workspace = workspace,
            Documents = documents,
            Notes = notes,
            ExportedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

        var result = new RenderResult(
            Content: json,
            ContentType: "application/json",
            Extension: ".json",
            FileName: $"{workspace.Name.ToLower().Replace(" ", "_")}_export.json"
        );

        return Task.FromResult(result);
    }
}

public class DocxWorkspaceWriter : IExportWriter
{
    public string Format => "DOCX";

    public Task<RenderResult> WriteWorkspaceAsync(Workspace workspace, IEnumerable<ResearchDocument> documents, IEnumerable<ResearchNote> notes, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"DOCX Workspace Document: {workspace.Name}");
        sb.AppendLine($"Description: {workspace.Description}");
        
        foreach (var doc in documents)
        {
            sb.AppendLine($"Document: {doc.Title}");
        }

        foreach (var note in notes)
        {
            sb.AppendLine($"Note: {note.Title}\n{note.Markdown}");
        }

        var result = new RenderResult(
            Content: sb.ToString(),
            ContentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Extension: ".docx",
            FileName: $"{workspace.Name.ToLower().Replace(" ", "_")}_export.docx"
        );

        return Task.FromResult(result);
    }
}
