using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IResearchService _researchService;

    public SearchController(IResearchService researchService)
    {
        _researchService = researchService;
    }

    /// <summary>
    /// Executes a deterministic lexical search across Arabic and translations.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<EvidenceDossier>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? language = "en",
        [FromQuery] string? translations = null,
        [FromQuery] bool includeMetadata = true,
        [FromQuery] bool includeHighlights = true,
        [FromQuery] bool includeReasons = true,
        [FromQuery] bool includeDiagnostics = true,
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

        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Pagination parameters 'page' must be >= 1 and 'pageSize' must be between 1 and 100."
            });
        }

        // Parse translation languages filter
        var langs = translations?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var options = new SearchOptions(
            Languages: langs,
            Page: page,
            PageSize: pageSize,
            IncludeHighlights: includeHighlights,
            IncludeReasons: includeReasons,
            IncludeDiagnostics: includeDiagnostics,
            IncludeTranslations: true
        );

        var query = new SearchQuery(q, options);
        var dossier = await _researchService.SearchAsync(query, cancellationToken);
        
        return Ok(new ApiResponse<EvidenceDossier>(dossier));
    }

    /// <summary>
    /// Fetches a single reference (e.g. 2:255 or alias) as an EvidenceItem.
    /// </summary>
    [HttpGet("reference/{reference}")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceItem>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetByReference(string reference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Reference parameter cannot be empty."
            });
        }

        var item = await _researchService.GetReferenceAsync(reference, cancellationToken);
        if (item == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Reference '{reference}' could not be resolved or found in database."
            });
        }

        return Ok(new ApiResponse<EvidenceItem>(item));
    }

    /// <summary>
    /// Gets autocomplete search suggestions.
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ApiResponse<List<SearchSuggestionDto>>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetSuggestions([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Suggestion prefix parameter 'q' cannot be empty."
            });
        }

        var suggestions = await _researchService.GetSuggestionsAsync(q, cancellationToken);
        return Ok(new ApiResponse<List<SearchSuggestionDto>>(suggestions));
    }
}
