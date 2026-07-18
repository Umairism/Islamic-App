using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class RetrievalBehavior : IResearchPipelineBehavior
{
    private readonly IEvidenceRepository _repository;

    public RetrievalBehavior(IEvidenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Retrieval || 
            (executionContext.Iteration != null && executionContext.Iteration.State == PipelineState.AdditionalRetrieval))
        {
            EvidenceCorpus corpus;

            if (executionContext.Iteration != null && executionContext.Iteration.State == PipelineState.AdditionalRetrieval)
            {
                var existingCorpus = executionContext.Context.Input.Corpus ?? 
                                     await _repository.GetEvidenceAsync(executionContext.Context.Input.Query, cancellationToken);
                var newEvidences = new List<ResearchEvidence>(existingCorpus.Evidences);

                foreach (var gap in executionContext.Iteration.PendingGaps)
                {
                    var extraQuery = new QueryAnalysis(
                        new SearchRequest(gap.SearchTerms, ResearchLanguage.English, null, null, false, false, false),
                        null!,
                        ResearchLanguage.English,
                        null!,
                        null!,
                        null!
                    );
                    var extraCorpus = await _repository.GetEvidenceAsync(extraQuery, cancellationToken);
                    newEvidences.AddRange(extraCorpus.Evidences);
                }

                corpus = existingCorpus with { Evidences = newEvidences };
            }
            else
            {
                corpus = await _repository.GetEvidenceAsync(executionContext.Context.Input.Query, cancellationToken);
            }

            var updatedContext = executionContext.Context.WithCorpus(corpus);
            var updatedExecContext = executionContext
                .WithContext(updatedContext)
                .TransitionTo(PipelineStage.Deduplication);

            if (updatedExecContext.Iteration != null)
            {
                updatedExecContext = updatedExecContext.WithIteration(
                    updatedExecContext.Iteration with { State = PipelineState.Retrieving }
                );
            }

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
