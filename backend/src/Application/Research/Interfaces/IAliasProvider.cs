using System.Collections.Generic;

namespace IslamicApp.Application.Research.Interfaces;

public interface IAliasProvider
{
    bool TryResolveAlias(string alias, out string normalizedReference);
    IReadOnlyDictionary<string, string> GetAllAliases();
}
