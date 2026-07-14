using System.Text;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Export;

public class HtmlExportFormatter : IExportFormatter
{
    public string Format => "html";

    public string FormatDossier(ResearchDossier dossier)
    {
        if (dossier == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine($"<title>Research Dossier - {dossier.Query}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Outfit', sans-serif; margin: 40px; background-color: #0b1512; color: #e2e8f0; }");
        sb.AppendLine("h1 { color: #10b981; }");
        sb.AppendLine(".section { border: 1px solid #1f3f37; border-radius: 8px; padding: 20px; margin-bottom: 20px; background-color: #0f1d1a; }");
        sb.AppendLine(".evidence-item { margin-bottom: 25px; border-bottom: 1px dotted #1f3f37; padding-bottom: 15px; }");
        sb.AppendLine(".arabic { font-family: 'Amiri', serif; font-size: 24px; text-align: right; margin: 15px 0; color: #34d399; }");
        sb.AppendLine(".meta { font-size: 0.9em; color: #94a3b8; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"<h1>Research Dossier: {dossier.Query}</h1>");
        sb.AppendLine($"<p><strong>Summary</strong>: {dossier.Summary}</p>");

        foreach (var section in dossier.EvidenceSections)
        {
            if (section.Value.Count == 0) continue;

            sb.AppendLine($"<div class=\"section\">");
            sb.AppendLine($"<h2>Section: {section.Key}</h2>");

            foreach (var item in section.Value)
            {
                sb.AppendLine("<div class=\"evidence-item\">");
                sb.AppendLine($"<h3>Citation: {item.Reference}</h3>");
                sb.AppendLine($"<p class=\"arabic\">{item.PrimaryText}</p>");
                
                if (item.Translations.Count > 0)
                {
                    sb.AppendLine($"<p><em>Translation</em>: \"{item.Translations[0].Text}\" ({item.Translations[0].Translator})</p>");
                }

                sb.AppendLine($"<p class=\"meta\">Confidence: {item.Confidence.OverallConfidence}% | Authority: {item.Confidence.SourceAuthority}</p>");

                if (item.CrossReferences.Count > 0)
                {
                    sb.AppendLine("<h4>Related Cross-References:</h4><ul>");
                    foreach (var xr in item.CrossReferences)
                    {
                        sb.AppendLine($"<li>[{xr.Relationship}] {xr.Reference}: <em>{xr.Description}</em></li>");
                    }
                    sb.AppendLine("</ul>");
                }

                sb.AppendLine("</div>");
            }
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<h2>Provenance Tracking</h2><ul>");
        foreach (var p in dossier.ProvenanceList)
        {
            sb.AppendLine($"<li>Dataset: {p.DatasetName} (Version: {p.Version})</li>");
        }
        sb.AppendLine("</ul>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
