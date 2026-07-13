using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ISourceReferenceResolver
{
    bool TryResolve(string query, out EvidenceReference? reference);
}
