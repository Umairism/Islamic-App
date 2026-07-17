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

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ResearchController : ControllerBase
{
    private readonly IResearchService _researchService;
    private readonly IExportEngine _exportEngine;

    public ResearchController(IResearchService researchService, IExportEngine exportEngine)
    {
        _researchService = researchService;
        _exportEngine = exportEngine;
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
}

public record GraphNodeDto(string Id, string Label, string Type);
public record GraphEdgeDto(string Source, string Target, string Type, string Description);
public record GraphResponseDto(List<GraphNodeDto> Nodes, List<GraphEdgeDto> Edges);
