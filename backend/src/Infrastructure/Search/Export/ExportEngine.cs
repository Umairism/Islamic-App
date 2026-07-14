using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Export;

public class ExportEngine : IExportEngine
{
    private readonly IEnumerable<IExportFormatter> _formatters;

    public ExportEngine(IEnumerable<IExportFormatter> formatters)
    {
        _formatters = formatters;
    }

    public string Export(ResearchDossier dossier, string format)
    {
        if (dossier == null) throw new ArgumentNullException(nameof(dossier));
        
        string targetFormat = format?.Trim().ToLowerInvariant() ?? "json";
        
        var formatter = _formatters.FirstOrDefault(f => string.Equals(f.Format, targetFormat, StringComparison.OrdinalIgnoreCase));
        if (formatter == null)
        {
            throw new NotSupportedException($"Export format '{format}' is not supported.");
        }

        return formatter.FormatDossier(dossier);
    }
}
