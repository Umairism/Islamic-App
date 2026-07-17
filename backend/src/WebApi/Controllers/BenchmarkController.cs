using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IslamicApp.Application.Retrieval.Benchmark;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BenchmarkController : ControllerBase
{
    private readonly ISemanticBenchmarkRunner _benchmarkRunner;

    public BenchmarkController(ISemanticBenchmarkRunner benchmarkRunner)
    {
        _benchmarkRunner = benchmarkRunner;
    }

    /// <summary>
    /// Executes semantic retrieval benchmarks measuring precision, recall, MRR, and NDCG.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(BenchmarkResult), 200)]
    public async Task<IActionResult> RunBenchmark(CancellationToken cancellationToken)
    {
        var result = await _benchmarkRunner.RunEvaluationsAsync(cancellationToken);
        return Ok(result);
    }
}
