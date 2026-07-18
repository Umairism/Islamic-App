using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Semantic.Query;
using Microsoft.EntityFrameworkCore;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ResearchController : ControllerBase
{
    private readonly IResearchService _researchService;
    private readonly IExportEngine _exportEngine;
    private readonly IResearchPipeline _researchPipeline;
    private readonly IQueryAnalyzer _queryAnalyzer;
    private readonly IQueryRewriter _queryRewriter;
    private readonly IKnowledgeMemoryStore _memoryStore;
    private readonly IslamicApp.Infrastructure.Research.IResearchQueue _queue;
    private readonly IslamicApp.Infrastructure.Persistence.ApplicationDbContext _dbContext;

    public ResearchController(
        IResearchService researchService,
        IExportEngine exportEngine,
        IResearchPipeline researchPipeline,
        IQueryAnalyzer queryAnalyzer,
        IQueryRewriter queryRewriter,
        IKnowledgeMemoryStore memoryStore,
        IslamicApp.Infrastructure.Research.IResearchQueue queue,
        IslamicApp.Infrastructure.Persistence.ApplicationDbContext dbContext)
    {
        _researchService = researchService;
        _exportEngine = exportEngine;
        _researchPipeline = researchPipeline;
        _queryAnalyzer = queryAnalyzer;
        _queryRewriter = queryRewriter;
        _memoryStore = memoryStore;
        _queue = queue;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Generates a structured research dossier with dynamic cross-references and timing profile timeline.
    /// </summary>
    [HttpGet("dossier")]
    [ProducesResponseType(typeof(ApiResponse<ResearchDossier>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetDossier(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Search query parameter 'q' cannot be empty."
            });
        }

        var request = new SearchRequest(
            Query: q,
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(page, pageSize),
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: false
        );

        var dossier = await _researchService.ResearchAsync(request, cancellationToken);
        return Ok(new ApiResponse<ResearchDossier>(dossier));
    }

    /// <summary>
    /// Exports the research dossier into the requested format (json, markdown, html).
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Export(
        [FromQuery] string q,
        [FromQuery] string format = "markdown",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Search query parameter 'q' cannot be empty."
            });
        }

        var request = new SearchRequest(
            Query: q,
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(1, 20),
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: false
        );
        var dossier = await _researchService.ResearchAsync(request, cancellationToken);

        try
        {
            string output = _exportEngine.Export(dossier, format);
            string contentType = format.ToLowerInvariant() switch
            {
                "json" => "application/json",
                "html" => "text/html",
                _ => "text/markdown"
            };

            string extension = format.ToLowerInvariant() switch
            {
                "json" => "json",
                "html" => "html",
                _ => "md"
            };

            var fileBytes = System.Text.Encoding.UTF8.GetBytes(output);
            return File(fileBytes, contentType, $"research-dossier-{q.Replace(" ", "-").ToLowerInvariant()}.{extension}");
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Format Not Supported",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Returns the dynamically built knowledge graph of nodes and edges for rendering.
    /// </summary>
    [HttpGet("graph")]
    [ProducesResponseType(typeof(ApiResponse<GraphResponseDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetGraph([FromQuery] string q, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Search query parameter 'q' cannot be empty."
            });
        }

        var request = new SearchRequest(
            Query: q,
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(1, 20),
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: false
        );
        var dossier = await _researchService.ResearchAsync(request, cancellationToken);

        var nodes = new List<GraphNodeDto>();
        var edges = new List<GraphEdgeDto>();

        nodes.Add(new GraphNodeDto("query", dossier.Query, "Query"));

        foreach (var section in dossier.EvidenceSections.Values)
        {
            foreach (var item in section)
            {
                string nodeId = item.Reference;
                nodes.Add(new GraphNodeDto(nodeId, item.Reference, item.Source.ToString()));
                edges.Add(new GraphEdgeDto("query", nodeId, "Matches", "Relevancy Match"));

                foreach (var xr in item.CrossReferences)
                {
                    string targetId = xr.Reference;
                    nodes.Add(new GraphNodeDto(targetId, xr.Reference, xr.Source.ToString()));
                    edges.Add(new GraphEdgeDto(nodeId, targetId, xr.Relationship.ToString(), xr.Description));
                }
            }
        }

        var response = new GraphResponseDto(
            Nodes: nodes.GroupBy(n => n.Id).Select(g => g.First()).ToList(),
            Edges: edges.GroupBy(e => new { e.Source, e.Target, e.Type }).Select(g => g.First()).ToList()
        );

        return Ok(new ApiResponse<GraphResponseDto>(response));
    }

    /// <summary>
    /// Lists all supported semantic relationships in the research knowledge graph.
    /// </summary>
    [HttpGet("relationships")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
    public IActionResult GetRelationships()
    {
        var list = Enum.GetNames(typeof(EvidenceRelationshipType)).ToList();
        return Ok(new ApiResponse<List<string>>(list));
    }

    /// <summary>
    /// Executes end-to-end evidence retrieval, analysis, AI reasoning, structured validation, explainability pathing, and async document rendering.
    /// </summary>
    [HttpPost("synthesize")]
    [ProducesResponseType(typeof(ApiResponse<ResearchResult>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Synthesize([FromBody] SynthesizeRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Search query parameter cannot be empty."
            });
        }

        var searchReq = new SearchRequest(
            Query: request.Query,
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(1, 20),
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: true
        );

        var queryAnalysis = await _queryAnalyzer.AnalyzeAsync(searchReq);
        var rewritten = await _queryRewriter.RewriteAsync(request.Query, cancellationToken);
        queryAnalysis = queryAnalysis with { SemanticQuery = rewritten };

        var pipeResult = await _researchPipeline.ExecuteAsync(queryAnalysis, cancellationToken);
        if (!pipeResult.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Pipeline Failed",
                Detail = pipeResult.Error!.Message
            });
        }

        var execCtx = pipeResult.Value!;
        var response = new ResearchResult(
            ExecutionContext: execCtx,
            Session: execCtx.Session!,
            Reasoning: execCtx.Reasoning!,
            Validation: execCtx.Validation!,
            Explainability: execCtx.Explainability!,
            Outputs: execCtx.RenderedOutputs?.ToList() ?? new List<RenderResult>()
        );

        return Ok(new ApiResponse<ResearchResult>(response));
    }

    /// <summary>
    /// Executes research scoped to a workspace.
    /// </summary>
    [HttpPost("/api/v1/workspaces/{id}/research")]
    [ProducesResponseType(typeof(ApiResponse<ResearchResult>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> WorkspaceResearch(
        [FromRoute] Guid id,
        [FromBody] SynthesizeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Search query parameter cannot be empty."
            });
        }

        var searchReq = new SearchRequest(
            Query: request.Query,
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(1, 20),
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: true
        );

        var queryAnalysis = await _queryAnalyzer.AnalyzeAsync(searchReq);
        var rewritten = await _queryRewriter.RewriteAsync(request.Query, cancellationToken);
        queryAnalysis = queryAnalysis with { SemanticQuery = rewritten };

        var pipeResult = await _researchPipeline.ExecuteAsync(queryAnalysis, cancellationToken);
        if (!pipeResult.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Pipeline Failed",
                Detail = pipeResult.Error!.Message
            });
        }

        var execCtx = pipeResult.Value!;
        if (execCtx.Metadata != null)
        {
            execCtx = execCtx.WithMetadata(execCtx.Metadata with { WorkspaceId = id });
        }

        var response = new ResearchResult(
            ExecutionContext: execCtx,
            Session: execCtx.Session!,
            Reasoning: execCtx.Reasoning!,
            Validation: execCtx.Validation!,
            Explainability: execCtx.Explainability!,
            Outputs: execCtx.RenderedOutputs?.ToList() ?? new List<RenderResult>()
        );

        return Ok(new ApiResponse<ResearchResult>(response));
    }

    /// <summary>
    /// Retrieves active memory entries for a workspace.
    /// </summary>
    [HttpGet("/api/v1/workspaces/{id}/memory")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MemoryEntry>>), 200)]
    public async Task<IActionResult> GetWorkspaceMemory([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var memories = await _memoryStore.GetWorkspaceMemoriesAsync(id, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MemoryEntry>>(memories));
    }

    /// <summary>
    /// Appends a continuation query leveraging workspace memory context history.
    /// </summary>
    [HttpPost("/api/v1/workspaces/{id}/continue")]
    [ProducesResponseType(typeof(ApiResponse<ResearchResult>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ContinueResearch(
        [FromRoute] Guid id,
        [FromBody] SynthesizeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Search query parameter cannot be empty."
            });
        }

        var searchReq = new SearchRequest(
            Query: request.Query,
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(1, 20),
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: true
        );

        var queryAnalysis = await _queryAnalyzer.AnalyzeAsync(searchReq);
        var rewritten = await _queryRewriter.RewriteAsync(request.Query, cancellationToken);
        queryAnalysis = queryAnalysis with { SemanticQuery = rewritten };

        var pipeResult = await _researchPipeline.ExecuteAsync(queryAnalysis, cancellationToken: cancellationToken);
        if (!pipeResult.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Pipeline Failed",
                Detail = pipeResult.Error!.Message
            });
        }

        var execCtx = pipeResult.Value!;
        if (execCtx.Metadata != null)
        {
            execCtx = execCtx.WithMetadata(execCtx.Metadata with { WorkspaceId = id });
        }

        var response = new ResearchResult(
            ExecutionContext: execCtx,
            Session: execCtx.Session!,
            Reasoning: execCtx.Reasoning!,
            Validation: execCtx.Validation!,
            Explainability: execCtx.Explainability!,
            Outputs: execCtx.RenderedOutputs?.ToList() ?? new List<RenderResult>()
        );

        return Ok(new ApiResponse<ResearchResult>(response));
    }

    /// <summary>
    /// Starts asynchronous background research processing on a workspace query.
    /// </summary>
    [HttpPost("/api/v1/workspaces/{id}/research/start")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> StartResearch([FromRoute] Guid id, [FromBody] SynthesizeRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Search query parameter cannot be empty."
            });
        }

        var session = new IslamicApp.Infrastructure.Persistence.Entities.ResearchSessionEntity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = id,
            Title = request.Query.Length > 50 ? request.Query.Substring(0, 47) + "..." : request.Query,
            Query = request.Query,
            CreatedAt = DateTimeOffset.UtcNow,
            Methodology = "Thematic",
            Language = "English",
            ConfidenceValue = 1.0,
            ConfidenceLevel = "Normal",
            Status = "Queued",
            CurrentStage = "Queueing"
        };

        _dbContext.ResearchSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        // Enqueue job asynchronously
        await _queue.EnqueueAsync(new IslamicApp.Infrastructure.Research.ResearchJob(session.Id, id, DateTimeOffset.UtcNow));

        return Accepted(new
        {
            sessionId = session.Id,
            status = "queued",
            hubUrl = "/hubs/research",
            estimatedStages = 13
        });
    }

    /// <summary>
    /// Cancels active research session processing.
    /// </summary>
    [HttpPost("/api/v1/research/{id}/cancel")]
    public async Task<IActionResult> CancelResearch([FromRoute] Guid id)
    {
        var session = await _dbContext.ResearchSessions.FindAsync(id);
        if (session == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Research session {id} not found."
            });
        }

        if (session.Status == "Completed" || session.Status == "Failed" || session.Status == "Cancelled")
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid Action",
                Detail = $"Cannot cancel research session in state {session.Status}."
            });
        }

        session.Status = "Cancelled";
        session.CurrentStage = "Cancelled";
        
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Status = 409,
                Title = "Concurrency Conflict",
                Detail = "The session has been updated by another process."
            });
        }

        return Ok(new { success = true, message = "Cancellation requested." });
    }

    /// <summary>
    /// Retrieves active progress status and metrics of a research session.
    /// </summary>
    [HttpGet("/api/v1/research/{id}/status")]
    public async Task<IActionResult> GetResearchStatus([FromRoute] Guid id)
    {
        var session = await _dbContext.ResearchSessions
            .Include(s => s.Iterations)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Research session {id} not found."
            });
        }

        return Ok(new
        {
            sessionId = session.Id,
            status = session.Status,
            stage = session.CurrentStage,
            iterationCount = session.Iterations.Count,
            updatedAt = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Retrieves the completed/versioned research results.
    /// </summary>
    [HttpGet("/api/v1/research/{id}/result")]
    public async Task<IActionResult> GetResearchResult([FromRoute] Guid id)
    {
        var result = await _dbContext.ResearchResults
            .Where(r => r.ResearchSessionId == id)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync();

        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Research result for session {id} not found."
            });
        }

        var citationsList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(result.CitationsJson) ?? new List<string>();

        return Ok(new
        {
            resultId = result.Id,
            sessionId = result.ResearchSessionId,
            answer = result.AnswerText,
            confidence = result.ConfidenceScore,
            citations = citationsList,
            version = result.Version,
            isFinal = result.IsFinal,
            generatedAt = result.GeneratedAt
        });
    }
}

public record SynthesizeRequestDto(string Query);
public record GraphNodeDto(string Id, string Label, string Type);
public record GraphEdgeDto(string Source, string Target, string Type, string Description);
public record GraphResponseDto(List<GraphNodeDto> Nodes, List<GraphEdgeDto> Edges);
