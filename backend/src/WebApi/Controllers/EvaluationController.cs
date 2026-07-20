using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Infrastructure.Persistence;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[Route("api/v1/research")]
public class EvaluationController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public EvaluationController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{sessionId:guid}/evaluation")]
    public async Task<IActionResult> GetEvaluation(Guid sessionId, CancellationToken cancellationToken)
    {
        var evalEntity = await _dbContext.ResearchEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ResearchSessionId == sessionId, cancellationToken);

        if (evalEntity == null)
        {
            return NotFound(new { message = $"No evaluation report found for research session '{sessionId}'." });
        }

        return Ok(new
        {
            sessionId = evalEntity.ResearchSessionId,
            overallScore = evalEntity.OverallScore,
            evidenceCoverage = evalEntity.EvidenceCoverage,
            citationAccuracy = evalEntity.CitationAccuracy,
            reasoningConsistency = evalEntity.ReasoningConsistency,
            sourceDiversity = evalEntity.SourceDiversity,
            evaluationVersion = evalEntity.EvaluationVersion,
            createdAt = evalEntity.CreatedAt,
            metrics = JsonSerializer.Deserialize<object>(evalEntity.MetricsJson),
            findings = JsonSerializer.Deserialize<object>(evalEntity.FindingsJson)
        });
    }
}
