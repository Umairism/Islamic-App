using Microsoft.AspNetCore.Mvc;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[Route("api/version")]
public class VersionController : ControllerBase
{
    /// <summary>
    /// Returns the current API version, build metadata, and loaded dataset version info.
    /// </summary>
    [HttpGet]
    public IActionResult GetVersion()
    {
        return Ok(new
        {
            api = "1.0",
            dataset = "Quran 3.1.2",
            build = "0.2.0"
        });
    }
}
