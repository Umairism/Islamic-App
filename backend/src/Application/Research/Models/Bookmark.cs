using System;

namespace IslamicApp.Application.Research.Models;

public record Bookmark(
    Guid Id,
    Guid WorkspaceId,
    Guid ResearchSessionId,
    string ReferenceId,
    string Comment,
    DateTimeOffset CreatedAt
);
