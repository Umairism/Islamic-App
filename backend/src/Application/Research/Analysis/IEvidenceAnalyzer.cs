using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IEvidenceAnalyzer
{
    EvidenceClassification Classify(ResearchEvidence evidence);
}
