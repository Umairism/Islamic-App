using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/quran/[controller]")]
public class VersesController : ControllerBase
{
    private readonly IEvidenceService _evidenceService;

    public VersesController(IEvidenceService evidenceService)
    {
        _evidenceService = evidenceService;
    }

    /// <summary>
    /// Retrieve a specific verse (ayah) by Surah number and Ayah number, including base Arabic text, English transliteration, canonical reference, and translations.
    /// </summary>
    /// <param name="surah">The Surah number (1-114).</param>
    /// <param name="ayah">The Ayah number (1-indexed inside the Surah).</param>
    /// <param name="translations">Optional comma-separated list of language codes to filter translations (e.g. "en,ur").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{surah:int}/{ayah:int}")]
    [ProducesResponseType(typeof(ApiResponse<VerseDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetVerse(int surah, int ayah, [FromQuery] string? translations, CancellationToken cancellationToken)
    {
        var langs = string.IsNullOrWhiteSpace(translations)
            ? new List<string>()
            : translations.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

        var verse = await _evidenceService.GetEvidenceByReferenceAsync($"{surah}:{ayah}", langs, cancellationToken);
        return Ok(new ApiResponse<VerseDto>(verse));
    }
}
