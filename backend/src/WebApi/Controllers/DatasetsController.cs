using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]")]
public class DatasetsController : ControllerBase
{
    private readonly IDatasetRepository _datasetRepository;

    public DatasetsController(IDatasetRepository datasetRepository)
    {
        _datasetRepository = datasetRepository;
    }

    /// <summary>
    /// Retrieve list of registered and imported dataset versions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiListResponse<DatasetDto>), 200)]
    public async Task<IActionResult> GetDatasets([FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var list = await _datasetRepository.GetDatasetsAsync(pagination, cancellationToken);
        var total = await _datasetRepository.GetTotalCountAsync(cancellationToken);
        
        var metadata = new PaginationMetadata(pagination.Page, pagination.PageSize, total);
        return Ok(new ApiListResponse<DatasetDto>(list, metadata));
    }
}
