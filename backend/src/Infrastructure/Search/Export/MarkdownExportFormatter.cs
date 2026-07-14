using System;
using System.Text;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Export;

public class MarkdownExportFormatter : IExportFormatter
{
    public string Format => "markdown";

    public string FormatDossier(ResearchDossier dossier)
    {
        if (dossier == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"# Research Dossier: {dossier.Query}");
        sb.AppendLine();
        sb.AppendLine($"> **Summary**: {dossier.Summary}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var section in dossier.EvidenceSections)
        {
            if (section.Value.Count == 0) continue;

            sb.AppendLine($"## Section: {section.Key}");
            sb.AppendLine();

            foreach (var item in section.Value)
            {
                sb.AppendLine($"### Citation: {item.Reference}");
                sb.AppendLine($"- **Source**: {item.Source} ({item.Collection})");
                sb.AppendLine($"- **Text (Arabic)**: {item.PrimaryText}");
                
                if (item.Translations.Count > 0)
                {
                    sb.AppendLine($"- **Translation**: \"{item.Translations[0].Text}\" (Translator: {item.Translations[0].Translator})");
                }

                sb.AppendLine($"- **Confidence**: {item.Confidence.OverallConfidence}% (Authority: {item.Confidence.SourceAuthority})");

                if (item.CrossReferences.Count > 0)
                {
                    sb.AppendLine($"- **Cross-References**:");
                    foreach (var xr in item.CrossReferences)
                    {
                        sb.AppendLine($"  - [{xr.Relationship}] {xr.Reference}: *{xr.Description}*");
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine("## Provenance Metadata");
        foreach (var p in dossier.ProvenanceList)
        {
            sb.AppendLine($"- **Dataset**: {p.DatasetName} (Version: {p.Version}, Checksum: {p.Checksum})");
        }

        return sb.ToString();
    }
}
