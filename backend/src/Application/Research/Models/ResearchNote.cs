using System;

namespace IslamicApp.Application.Research.Models;

public record ResearchNote(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string Markdown,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
