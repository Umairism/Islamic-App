using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IEvidenceRepository
{
    Task<EvidenceCorpus> GetEvidenceAsync(
        QueryAnalysis query,
        CancellationToken cancellationToken);
}
