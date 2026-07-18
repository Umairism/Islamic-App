using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public record KnowledgeMemoryItem(
    Guid SessionId,
    string Text,
    double RelevanceScore,
    DateTimeOffset CreatedAt
);

public interface IKnowledgeMemory
{
    Task StoreAsync(ResearchResult result, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeMemoryItem>> SearchAsync(string query, CancellationToken cancellationToken);
}
