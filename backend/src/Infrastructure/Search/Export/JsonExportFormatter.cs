using System.Text.Json;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Export;

public class JsonExportFormatter : IExportFormatter
{
    public string Format => "json";

    public string FormatDossier(ResearchDossier dossier)
    {
        if (dossier == null) return "{}";
        return JsonSerializer.Serialize(dossier, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
