using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IEvidenceDeduplicator
{
    EvidenceCorpus Deduplicate(EvidenceCorpus corpus);
}
