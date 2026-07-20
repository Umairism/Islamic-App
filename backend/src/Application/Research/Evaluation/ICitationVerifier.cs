using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Evaluation;

public interface ICitationVerifier
{
    Task<CitationVerificationResult> VerifyAsync(
        ReferenceId referenceId,
        string claimText,
        EvidenceCorpus corpus,
        CancellationToken cancellationToken = default);
}
