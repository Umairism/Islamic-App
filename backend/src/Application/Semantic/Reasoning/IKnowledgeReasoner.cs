using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Semantic.Reasoning;

public record ResearchSummary(string Content, string Methodology);

public interface IKnowledgeReasoner
{
    Task<ResearchSummary> GenerateSummaryAsync(
        EvidenceDossier dossier,
        CancellationToken cancellationToken);
}
