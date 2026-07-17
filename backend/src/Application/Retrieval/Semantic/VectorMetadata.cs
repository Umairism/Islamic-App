using System;
using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Retrieval.Semantic;

public record VectorStorageMetadata(
    string DatasetId,
    string Checksum,
    DateTime ImportedAt
);

public record VectorRetrievalMetadata(
    ResearchLanguage Language,
    IReadOnlyList<string> Topics,
    IReadOnlyList<string> Keywords
);

public record VectorMetadata(
    VectorStorageMetadata Storage,
    VectorRetrievalMetadata Retrieval
);
