using System;

namespace IslamicApp.Application.Research.Models;

public record ResearchCollection(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt
);
