using System;

namespace IslamicApp.Application.Research.Memory;

public interface IMemoryDecayStrategy
{
    double GetDecayFactor(DateTimeOffset createdAt, string workspaceType);
}
