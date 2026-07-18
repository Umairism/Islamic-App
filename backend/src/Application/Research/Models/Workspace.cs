using System;

namespace IslamicApp.Application.Research.Models;

public record Workspace(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt
);
