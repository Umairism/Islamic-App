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

    public ResearchController(
        IResearchService researchService,
        IExportEngine exportEngine,
        IResearchPipeline researchPipeline,
        IQueryAnalyzer queryAnalyzer,
        IQueryRewriter queryRewriter,
        IKnowledgeMemoryStore memoryStore)
    {
        _researchService = researchService;
        _exportEngine = exportEngine;
        _researchPipeline = researchPipeline;
        _queryAnalyzer = queryAnalyzer;
        _queryRewriter = queryRewriter;
        _memoryStore = memoryStore;
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
}

public record SynthesizeRequestDto(string Query);
public record GraphNodeDto(string Id, string Label, string Type);
public record GraphEdgeDto(string Source, string Target, string Type, string Description);
public record GraphResponseDto(List<GraphNodeDto> Nodes, List<GraphEdgeDto> Edges);
