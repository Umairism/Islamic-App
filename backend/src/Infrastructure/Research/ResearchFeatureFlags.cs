using Microsoft.Extensions.Configuration;
using IslamicApp.Application.Research.Analysis;

namespace IslamicApp.Infrastructure.Research;

public class ResearchFeatureFlags : IResearchFeatureFlags
{
    private readonly IConfiguration _configuration;

    public ResearchFeatureFlags(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool EnableReasoning => GetFlag("FeatureFlags:EnableReasoning", true);
    public bool EnableValidation => GetFlag("FeatureFlags:EnableValidation", true);
    public bool EnableExplainability => GetFlag("FeatureFlags:EnableExplainability", true);
    public bool EnableWorkspacePersistence => GetFlag("FeatureFlags:EnableWorkspacePersistence", true);
    public bool EnableExport => GetFlag("FeatureFlags:EnableExport", true);
    public bool EnableKnowledgeMemory => GetFlag("FeatureFlags:EnableKnowledgeMemory", true);

    private bool GetFlag(string key, bool defaultValue)
    {
        var value = _configuration[key];
        return string.IsNullOrEmpty(value) ? defaultValue : (bool.TryParse(value, out var result) ? result : defaultValue);
    }
}
