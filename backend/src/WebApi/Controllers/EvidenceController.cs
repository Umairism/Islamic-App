using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EvidenceController : ControllerBase
{
    private readonly IResearchService _researchService;

    public EvidenceController(IResearchService researchService)
    {
        _researchService = researchService;
    }

    /// <summary>
    /// Fetches a single evidence item by its reference string (e.g. 2:255 or bukhari 54).
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceItem>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetEvidenceById(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Evidence ID/Reference parameter cannot be empty."
            });
        }

        // Resolves reference using the resolution pipeline
        var item = await _researchService.GetReferenceAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Evidence reference '{id}' could not be found."
            });
        }

        return Ok(new ApiResponse<EvidenceItem>(item));
    }
}
