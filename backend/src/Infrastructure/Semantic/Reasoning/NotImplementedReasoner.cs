using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Semantic.Reasoning;

namespace IslamicApp.Infrastructure.Semantic.Reasoning;

public class NotImplementedReasoner : IKnowledgeReasoner
{
    public Task<ResearchSummary> GenerateSummaryAsync(
        EvidenceDossier dossier,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new ResearchSummary(
            Content: "Reasoner is pending active LLM interface bindings.",
            Methodology: "Determines relevance via reciprocal rank fusion and local ontologies expansion. Summarizer not active."
        ));
    }
}
