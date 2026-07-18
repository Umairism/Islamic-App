namespace IslamicApp.Application.Research.Analysis;

public interface IResearchFeatureFlags
{
    bool EnableReasoning { get; }
    bool EnableValidation { get; }
    bool EnableExplainability { get; }
    bool EnableWorkspacePersistence { get; }
    bool EnableExport { get; }
    bool EnableKnowledgeMemory { get; }
}
