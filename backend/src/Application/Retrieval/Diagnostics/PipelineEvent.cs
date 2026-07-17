using System;
using System.Collections.Generic;

namespace IslamicApp.Application.Retrieval.Diagnostics;

public record PipelineEvent(
    string Stage,
    string Action,
    TimeSpan Duration,
    IReadOnlyDictionary<string, string> Metadata
);
