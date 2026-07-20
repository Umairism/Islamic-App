using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Research.Evaluation;
using IslamicApp.Infrastructure.Persistence;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
public class DossierController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDossierGenerator _dossierGenerator;
    private readonly IResearchEvaluator _evaluator;

    public DossierController(
        ApplicationDbContext dbContext,
        IDossierGenerator dossierGenerator,
        IResearchEvaluator evaluator)
    {
        _dbContext = dbContext;
        _dossierGenerator = dossierGenerator;
        _evaluator = evaluator;
    }

    [HttpPost("api/v1/research/{sessionId:guid}/dossier")]
    public async Task<IActionResult> GenerateDossier(Guid sessionId, CancellationToken cancellationToken)
    {
        var dossierEntity = await _dbContext.ResearchDossiers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ResearchSessionId == sessionId, cancellationToken);

        if (dossierEntity != null)
        {
            return Ok(new
            {
                dossierId = dossierEntity.Id,
                sessionId = dossierEntity.ResearchSessionId,
                format = dossierEntity.Format,
                contentHash = dossierEntity.ContentHash,
                createdAt = dossierEntity.CreatedAt
            });
        }

        return NotFound(new { message = $"No research session found with ID '{sessionId}'." });
    }

    [HttpGet("api/v1/dossiers/{id:guid}")]
    public async Task<IActionResult> GetDossier(Guid id, CancellationToken cancellationToken)
    {
        var dossier = await _dbContext.ResearchDossiers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id || d.ResearchSessionId == id, cancellationToken);

        if (dossier == null)
        {
            return NotFound(new { message = $"Dossier '{id}' not found." });
        }

        string content = string.Empty;
        if (System.IO.File.Exists(dossier.StoragePath))
        {
            content = await System.IO.File.ReadAllTextAsync(dossier.StoragePath, cancellationToken);
        }

        return Ok(new
        {
            dossierId = dossier.Id,
            sessionId = dossier.ResearchSessionId,
            format = dossier.Format,
            contentHash = dossier.ContentHash,
            markdownContent = content,
            createdAt = dossier.CreatedAt
        });
    }
}
