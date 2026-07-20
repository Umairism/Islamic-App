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
        var refId = prompt.Variables.AllowedReferences?.Select(r => r.Value).FirstOrDefault()
                    ?? prompt.Variables.Snippets?.Select(s => s.Reference.Value).FirstOrDefault()
                    ?? "2:255";
        string mockJson;

        if (query.Contains("alcohol") || query.Contains("wine"))
        {
            mockJson = $$"""
            {
                "summary": "The evolution of legal rulings on intoxicants progressed in stages, starting with general warnings and culminating in complete prohibition.",
                "claims": [
                    {
                        "statement": "Alcohol consumption was prohibited in stages.",
                        "supportingEvidence": ["{{refId}}"],
                        "confidence": 0.95,
                        "claimType": "LegalRuling",
                        "origin": "DirectEvidence"
                    }
                ],
                "findings": [
                    {
                        "section": "Chronological Prohibitions",
                        "heading": "Gradual Abrogation",
                        "details": "Evidence shows that earlier verses permitted wine but warned against its harm, which was later abrogated by absolute prohibition.",
                        "citedReferences": ["{{refId}}"]
                    }
                ],
                "limitations": [
                    {
                        "limitationDescription": "The prohibition was historical and gradual.",
                        "impact": "Rulings prior to final abrogation must not be followed in practice.",
                        "affectedEvidences": ["{{refId}}"]
                    }
                ]
            }
            """;
        }
        else
        {
            mockJson = $$"""
            {
                "summary": "Circumcision (Khitan) is established in Islamic jurisprudence as an act of Fitrah (natural disposition) and a Sunnah of Prophet Ibrahim (peace be upon him).",
                "claims": [
                    {
                        "statement": "Circumcision is a key practice of the Fitrah according to authentic Hadith narrations.",
                        "supportingEvidence": ["{{refId}}"],
                        "confidence": 0.95,
                        "claimType": "LegalRuling",
                        "origin": "DirectEvidence"
                    }
                ],
                "findings": [
                    {
                        "section": "Sunnah and Fitrah",
                        "heading": "Islamic Ruling on Circumcision",
                        "details": "Islamic scholars agree that circumcision is among the practices of Fitrah, strongly emphasized in prophetic tradition.",
                        "citedReferences": ["{{refId}}"]
                    }
                ],
                "limitations": [
                    {
                        "limitationDescription": "None identified.",
                        "impact": "None.",
                        "affectedEvidences": []
                    }
                ]
            }
            """;
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
