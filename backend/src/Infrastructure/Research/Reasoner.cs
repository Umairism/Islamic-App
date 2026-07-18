using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research;

public class Reasoner : IReasoner
{
    private readonly IPromptService _promptService;
    private readonly IEnumerable<ITextGenerationProvider> _providers;
    private readonly IReasoningParser _parser;
    private readonly IResearchValidator _validator;
    private readonly IExplainabilityBuilder _explainabilityBuilder;
    private readonly IOutputGuard _outputGuard;

    public Reasoner(
        IPromptService promptService,
        IEnumerable<ITextGenerationProvider> providers,
        IReasoningParser parser,
        IResearchValidator validator,
        IExplainabilityBuilder explainabilityBuilder,
        IOutputGuard outputGuard)
    {
        _promptService = promptService;
        _providers = providers;
        _parser = parser;
        _validator = validator;
        _explainabilityBuilder = explainabilityBuilder;
        _outputGuard = outputGuard;
    }

    public async Task<Result<ResearchResult>> ReasonAsync(
        ResearchContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Build prompt
            var prompt = await _promptService.BuildPromptAsync(context, cancellationToken);
            
            // 2. Resolve provider (Use MockProvider if OpenAI/Gemini stubs aren't explicitly requested)
            // Register providers and search for target, fallback to first available (e.g. MockProvider)
            var activeProviderName = "MockProvider"; // Set MockProvider as default
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(activeProviderName, StringComparison.OrdinalIgnoreCase))
                           ?? _providers.FirstOrDefault()
                           ?? throw new InvalidOperationException("No text generation providers registered in DI container.");

            var options = new GenerationOptions(
                Temperature: 0.2,
                TopP: 0.9,
                MaxTokens: 4096,
                Format: ResponseFormat.Json
            );

            // 3. Track started timestamp
            var startedAt = DateTimeOffset.UtcNow;
            
            // 4. Generate structured content
            var response = await provider.GenerateAsync(prompt, options, cancellationToken);
            var completedAt = DateTimeOffset.UtcNow;

            // 5. Track reasoning session DTO
            var session = new ReasoningSession(
                SessionId: Guid.NewGuid(),
                Prompt: prompt,
                Response: response,
                Metadata: response.Metadata,
                StartedAt: startedAt,
                CompletedAt: completedAt
            );

            // 6. Parse output DTO
            var parseResult = _parser.Parse(response.Content, context, response.Metadata);
            if (!parseResult.IsSuccess)
            {
                return Result<ResearchResult>.Failure(parseResult.Error!);
            }

            var reasoning = parseResult.Value!;

            // 7. Validate claims, citations, and consistency
            var validation = _validator.ValidateAll(reasoning, context);

            // 8. Build explainability path map
            var explainability = _explainabilityBuilder.BuildMap(reasoning, context);

            // 9. Evaluate publishability against Output Guard rules
            var executionContext = new ResearchExecutionContext(
                Context: context,
                Events: System.Collections.Immutable.ImmutableList<Application.Research.Events.IDomainEvent>.Empty,
                CurrentStage: PipelineStage.Completed,
                StageExecutions: System.Collections.Immutable.ImmutableList<PipelineStageExecution>.Empty
            );

            var publishResult = _outputGuard.EvaluatePublishability(
                executionContext,
                session,
                reasoning,
                validation,
                explainability
            );

            return publishResult;
        }
        catch (Exception ex)
        {
            return Result<ResearchResult>.Failure(new Error(
                Code: "ReasoningError",
                Message: $"An unexpected error occurred during reasoning phase: {ex.Message}",
                Severity: ErrorSeverity.Error
            ));
        }
    }
}
