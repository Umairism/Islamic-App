using System;

namespace IslamicApp.Application.Research.Models;

public record ResearchDocument(
    Guid Id,
    Guid SessionId,
    string Title,
    Guid WorkspaceId,
    Guid? CurrentRevisionId,
    DateTimeOffset CreatedAt
);
