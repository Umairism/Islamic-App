using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using IslamicApp.Application.DTOs;

namespace IslamicApp.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<WebApi.Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<WebApi.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var health = await response.Content.ReadFromJsonAsync<HealthCheckResponseDto>();
        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.Equal("Healthy", health.Database);
        Assert.Equal("Healthy", health.Dataset);
        Assert.False(string.IsNullOrWhiteSpace(health.CorrelationId));
        
        // Assert header
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task GetVersion_ReturnsVersionInfo()
    {
        var response = await _client.GetAsync("/api/version");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        
        Assert.Equal("1.0", root.GetProperty("api").GetString());
        Assert.Equal("Quran 3.1.2", root.GetProperty("dataset").GetString());
        Assert.Equal("0.2.0", root.GetProperty("build").GetString());
    }

    [Fact]
    public async Task GetVerse_AyatAlKursi_ReturnsCorrectData()
    {
        var response = await _client.GetAsync("/api/v1/quran/verses/2/255");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<VerseDto>>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        
        var verse = result.Data;
        Assert.NotNull(verse);
        Assert.Equal(2, verse.SurahNumber);
        Assert.Equal(255, verse.AyahNumber);
        
        Assert.NotNull(verse.Reference);
        Assert.Equal("Quran", verse.Reference.Type);
        Assert.Equal("2:255", verse.Reference.Reference);
        Assert.Equal(262, verse.Reference.GlobalIndex);
        
        // Check Arabic text exists
        Assert.Contains("لله", verse.ArabicCleaned);
        
        // Check translation exists
        Assert.NotEmpty(verse.Translations);
        var enTrans = verse.Translations.FirstOrDefault(t => t.Language == "en");
        Assert.NotNull(enTrans);
        Assert.Contains("Allah", enTrans.Text);
    }

    [Fact]
    public async Task GetSurah_InvalidSurah_ReturnsRfc7807ProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/quran/surahs/150");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        
        Assert.Equal("Not Found", root.GetProperty("title").GetString());
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("/api/v1/quran/surahs/150", root.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("correlationId").GetString()));
    }
}
