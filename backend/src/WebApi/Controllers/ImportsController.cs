using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]")]
public class ImportsController : ControllerBase
{
    private readonly IImportSessionRepository _importSessionRepository;

    public ImportsController(IImportSessionRepository importSessionRepository)
    {
        _importSessionRepository = importSessionRepository;
    }

    /// <summary>
    /// Retrieve list of ETL pipeline import session audit logs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiListResponse<ImportSessionDto>), 200)]
    public async Task<IActionResult> GetImportSessions([FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var list = await _importSessionRepository.GetImportSessionsAsync(pagination, cancellationToken);
        var total = await _importSessionRepository.GetTotalCountAsync(cancellationToken);
        
        var metadata = new PaginationMetadata(pagination.Page, pagination.PageSize, total);
        return Ok(new ApiListResponse<ImportSessionDto>(list, metadata));
    }
}
