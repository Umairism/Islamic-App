using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/quran/[controller]")]
public class SurahsController : ControllerBase
{
    private readonly IEvidenceService _evidenceService;

    public SurahsController(IEvidenceService evidenceService)
    {
        _evidenceService = evidenceService;
    }

    /// <summary>
    /// Retrieve a paginated list of all 114 Surahs and their metadata.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiListResponse<SurahDto>), 200)]
    public async Task<IActionResult> GetSurahs([FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var list = await _evidenceService.GetSurahsAsync(pagination, cancellationToken);
        var total = await _evidenceService.GetTotalSurahCountAsync(cancellationToken);
        
        var metadata = new PaginationMetadata(pagination.Page, pagination.PageSize, total);
        return Ok(new ApiListResponse<SurahDto>(list, metadata));
    }

    /// <summary>
    /// Retrieve metadata for a specific Surah by its Surah number (1-114).
    /// </summary>
    [HttpGet("{number:int}")]
    [ProducesResponseType(typeof(ApiResponse<SurahDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetSurahByNumber(int number, CancellationToken cancellationToken)
    {
        var surah = await _evidenceService.GetSurahByNumberAsync(number, cancellationToken);
        return Ok(new ApiResponse<SurahDto>(surah));
    }
}
