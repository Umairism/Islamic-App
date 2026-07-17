using System.Collections.Immutable;
using System.Threading;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Policies;

namespace IslamicApp.Application.Retrieval.Diagnostics;

public record RetrievalContext(
    QueryAnalysis Query,
    SemanticPolicy Policy,
    CancellationToken CancellationToken,
    ImmutableList<PipelineEvent> Events
);
