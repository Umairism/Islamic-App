using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface IExportEngine
{
    string Export(ResearchDossier dossier, string format);
}
