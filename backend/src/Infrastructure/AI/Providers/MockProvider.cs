using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.AI.Providers;

public class MockProvider : ITextGenerationProvider
{
    public string ProviderName => "MockProvider";
    public bool SupportsJsonMode => true;
    public bool SupportsStreaming => false;
    public bool SupportsSeed => true;
    public bool SupportsVision => false;
    public bool SupportsTools => false;

    public Task<GenerationResponse> GenerateAsync(
        ResearchPrompt prompt,
        GenerationOptions options,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        
        var query = prompt.Variables.Query.ToLowerInvariant();
        string mockJson;

        if (query.Contains("alcohol") || query.Contains("wine"))
        {
            mockJson = @"{
                ""summary"": ""The evolution of legal rulings on intoxicants progressed in stages, starting with general warnings and culminating in complete prohibition."",
                ""claims"": [
                    {
                        ""statement"": ""Alcohol consumption was prohibited in stages."",
                        ""supportingEvidence"": [""2:255""],
                        ""confidence"": { ""value"": 0.95 },
                        ""claimType"": ""LegalRuling"",
                        ""origin"": ""DirectEvidence""
                    }
                ],
                ""findings"": [
                    {
                        ""section"": ""Chronological Prohibitions"",
                        ""heading"": ""Gradual Abrogation"",
                        ""details"": ""Evidence shows that earlier verses permitted wine but warned against its harm, which was later abrogated by absolute prohibition."",
                        ""citedReferences"": [""2:255""]
                    }
                ],
                ""limitations"": [
                    {
                        ""limitationDescription"": ""The prohibition was historical and gradual."",
                        ""impact"": ""Rulings prior to final abrogation must not be followed in practice."",
                        ""affectedEvidences"": [""2:255""]
                    }
                ]
            }";
        }
        else
        {
            mockJson = @"{
                ""summary"": ""General theological synthesis of the provided references, discussing core values, ethical constraints, and general directives."",
                ""claims"": [
                    {
                        ""statement"": ""The primary source dictates ethical relations between individuals."",
                        ""supportingEvidence"": [""2:255""],
                        ""confidence"": { ""value"": 0.90 },
                        ""claimType"": ""Theological"",
                        ""origin"": ""DirectEvidence""
                    }
                ],
                ""findings"": [
                    {
                        ""section"": ""Theological Pillars"",
                        ""heading"": ""Ethics and Justice"",
                        ""details"": ""Evidence indicates that justice and ethics form the backbone of all relational references."",
                        ""citedReferences"": [""2:255""]
                    }
                ],
                ""limitations"": [
                    {
                        ""limitationDescription"": ""None identified."",
                        ""impact"": ""None."",
                        ""affectedEvidences"": []
                    }
                ]
            }";
        }

        sw.Stop();

        var meta = new GenerationMetadata(
            Provider: ProviderName,
            Model: "MockModel-V1",
            PromptTokens: 250,
            CompletionTokens: 180,
            Duration: sw.Elapsed,
            Cached: false,
            FinishReason: FinishReason.Stop
        );

        return Task.FromResult(new GenerationResponse(mockJson, meta));
    }
}
