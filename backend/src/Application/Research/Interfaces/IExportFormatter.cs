using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface IExportFormatter
{
    string Format { get; }
    string FormatDossier(ResearchDossier dossier);
}
