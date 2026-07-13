using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/quran/[controller]")]
public class TranslationsController : ControllerBase
{
    private readonly IEvidenceService _evidenceService;

    public TranslationsController(IEvidenceService evidenceService)
    {
        _evidenceService = evidenceService;
    }

    /// <summary>
    /// Retrieve a paginated list of supported languages and translators.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiListResponse<TranslationInfoDto>), 200)]
    public async Task<IActionResult> GetTranslations([FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var list = await _evidenceService.GetTranslationsAsync(pagination, cancellationToken);
        var total = await _evidenceService.GetTotalTranslationCountAsync(cancellationToken);
        
        var metadata = new PaginationMetadata(pagination.Page, pagination.PageSize, total);
        return Ok(new ApiListResponse<TranslationInfoDto>(list, metadata));
    }
}
