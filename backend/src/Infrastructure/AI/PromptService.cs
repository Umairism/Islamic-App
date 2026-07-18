using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Infrastructure.AI;

public class PromptService : IPromptService
{
    private readonly string _templatesDirectory;

    public PromptService()
    {
        // Resolve directory dynamically to support both web running and test execution environments
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var relativeDir = Path.Combine(baseDir, "PromptTemplates");
        
        if (Directory.Exists(relativeDir))
        {
            _templatesDirectory = relativeDir;
        }
        else
        {
            // Traverse upwards to look for backend/src/WebApi/PromptTemplates
            var current = baseDir;
            string? foundDir = null;
            for (int i = 0; i < 5; i++)
            {
                var parent = Directory.GetParent(current)?.FullName;
                if (parent == null) break;
                
                var testPath = Path.Combine(parent, "src", "WebApi", "PromptTemplates");
                if (Directory.Exists(testPath))
                {
                    foundDir = testPath;
                    break;
                }
                
                testPath = Path.Combine(parent, "backend", "src", "WebApi", "PromptTemplates");
                if (Directory.Exists(testPath))
                {
                    foundDir = testPath;
                    break;
                }
                
                current = parent;
            }
            
            _templatesDirectory = foundDir ?? baseDir; // Fallback to base dir if not found
        }
    }

    public async Task<ResearchPrompt> BuildPromptAsync(ResearchContext context, CancellationToken cancellationToken)
    {
        var methodology = context.Analysis?.Methodology.Type ?? ResearchMethodologyType.Thematic;
        var templateName = methodology.ToString().ToLowerInvariant();
        var templateFile = Path.Combine(_templatesDirectory, $"{templateName}.md");

        // Fallback to thematic if specific template doesn't exist
        if (!File.Exists(templateFile))
        {
            templateFile = Path.Combine(_templatesDirectory, "thematic.md");
        }

        string rawContent;
        if (File.Exists(templateFile))
        {
            rawContent = await File.ReadAllTextAsync(templateFile, cancellationToken);
        }
        else
        {
            // hardcoded fallback template if directory is completely missing during mock environments
            rawContent = GetHardcodedFallbackTemplate(templateName);
        }

        var (metadata, templateText) = ParseYamlFrontmatter(rawContent);

        var allowedReferences = context.Input.Corpus?.Evidences
            .Select(e => e.Reference)
            .Distinct()
            .ToList() ?? new List<ReferenceId>();

        var snippets = context.Input.Corpus?.Evidences.Select(e => new EvidenceSnippet(
            NodeId: new NodeId(ComputeNodeId(e.Source, e.Reference.Value)),
            DocumentId: e.Id,
            Reference: e.Reference,
            Content: e.Content,
            Classification: DetermineClassification(e.Source, context.Analysis?.Graph)
        )).ToList() ?? new List<EvidenceSnippet>();

        // Format variables
        var snippetsBuilder = new StringBuilder();
        foreach (var snip in snippets)
        {
            snippetsBuilder.AppendLine($"- [Node: {snip.NodeId.Value}] Reference: {snip.Reference.Value} ({snip.Classification})");
            snippetsBuilder.AppendLine($"  Content: {snip.Content}");
        }

        var referencesBuilder = new StringBuilder();
        foreach (var r in allowedReferences)
        {
            referencesBuilder.AppendLine($"- {r.Value}");
        }

        var queryStr = context.Input.Query.Query.Original;
        var snippetListStr = snippetsBuilder.ToString();
        var allowedRefsStr = referencesBuilder.ToString();

        // Separate system and user parts out of parsed template
        string systemPrompt = "You are a helpful assistant.";
        string userPrompt = templateText;

        var systemSplitIndex = templateText.IndexOf("System:", StringComparison.OrdinalIgnoreCase);
        var userSplitIndex = templateText.IndexOf("User:", StringComparison.OrdinalIgnoreCase);

        if (systemSplitIndex >= 0 && userSplitIndex > systemSplitIndex)
        {
            systemPrompt = templateText.Substring(systemSplitIndex + 7, userSplitIndex - (systemSplitIndex + 7)).Trim();
            userPrompt = templateText.Substring(userSplitIndex + 5).Trim();
        }

        var memoryBuilder = new StringBuilder();
        if (context.Memory != null && context.Memory.Selected.Count > 0)
        {
            memoryBuilder.AppendLine("Workspace Research Memory Context (Past Findings):");
            foreach (var mem in context.Memory.Selected)
            {
                memoryBuilder.AppendLine($"- Past Query: {mem.Query}");
                memoryBuilder.AppendLine($"  Summary: {mem.Summary}");
                if (mem.Claims.Count > 0)
                {
                    memoryBuilder.AppendLine("  Claims:");
                    foreach (var claim in mem.Claims)
                    {
                        memoryBuilder.AppendLine($"    * {claim}");
                    }
                }
            }
        }
        var memoryStr = memoryBuilder.ToString();

        systemPrompt = ReplaceTemplateVariables(systemPrompt, queryStr, snippetListStr, allowedRefsStr, memoryStr);
        userPrompt = ReplaceTemplateVariables(userPrompt, queryStr, snippetListStr, allowedRefsStr, memoryStr);

        var promptTemplate = new PromptTemplate(
            Name: metadata.GetValueOrDefault("name", templateName),
            Version: metadata.GetValueOrDefault("version", "1.0.0"),
            FilePath: templateFile,
            ConfigMetadata: metadata
        );

        var variables = new PromptVariables(
            Query: queryStr,
            Methodology: methodology,
            Snippets: snippets,
            AllowedReferences: allowedReferences
        );

        return new ResearchPrompt(
            Template: promptTemplate,
            Variables: variables,
            RenderedSystemPrompt: systemPrompt,
            RenderedUserPrompt: userPrompt
        );
    }

    private string ReplaceTemplateVariables(string text, string query, string snippetList, string allowedRefs, string memoryStr)
    {
        return text
            .Replace("{{Query}}", query)
            .Replace("{{SnippetList}}", snippetList)
            .Replace("{{AllowedReferences}}", allowedRefs)
            .Replace("{{WorkspaceMemory}}", memoryStr);
    }

    private (IReadOnlyDictionary<string, string> Metadata, string TemplateText) ParseYamlFrontmatter(string content)
    {
        var metadata = new Dictionary<string, string>();
        var templateText = content;

        if (content.StartsWith("---"))
        {
            var nextIndex = content.IndexOf("---", 3);
            if (nextIndex > 0)
            {
                var yamlSection = content.Substring(3, nextIndex - 3);
                templateText = content.Substring(nextIndex + 3).Trim();

                using var reader = new StringReader(yamlSection);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        metadata[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
        }

        return (metadata, templateText);
    }

    private string ComputeNodeId(EvidenceSource source, string reference)
    {
        var rawKey = $"{source.ToString().ToLowerInvariant()}:{reference.ToLowerInvariant()}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToBase64String(bytes)[..16];
    }

    private EvidenceClassification DetermineClassification(EvidenceSource source, EvidenceGraph? graph)
    {
        if (source == EvidenceSource.Quran) return EvidenceClassification.PrimarySource;
        if (source == EvidenceSource.Hadith) return EvidenceClassification.SecondarySource;
        return EvidenceClassification.Commentary;
    }

    private string GetHardcodedFallbackTemplate(string name)
    {
        return @"---
name: " + name + @"
version: 1.0.0
temperature: 0.2
maxTokens: 4096
responseSchema: reasoning_schema_v1.json
---
System: You are an expert Islamic researcher using " + name + @" analysis methodology. Your goal is to synthesize the provided evidence snippets to answer the user query: '{{Query}}'.
User: Snippets:
{{SnippetList}}
Allowed References:
{{AllowedReferences}}";
    }
}
