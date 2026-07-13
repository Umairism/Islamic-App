using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.IntegrationTests;

public class SearchApiIntegrationTests : IClassFixture<WebApplicationFactory<WebApi.Program>>
{
    private readonly HttpClient _client;

    public SearchApiIntegrationTests(WebApplicationFactory<WebApi.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_ReferenceQuery_ReturnsAyatAlKursi()
    {
        var response = await _client.GetAsync("/api/v1/search?q=2:255");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EvidenceDossier>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        var dossier = result.Data;
        Assert.Equal("2:255", dossier.ExecutionContext.OriginalQuery);
        Assert.Equal("ReferenceMatch", dossier.ExecutionContext.Strategy);
        
        var primary = dossier.Collections.FirstOrDefault(c => c.GroupName == "Primary Evidence")?.Items;
        Assert.NotNull(primary);
        Assert.NotEmpty(primary);
        
        var firstMatch = primary.First();
        Assert.Equal(EvidenceSource.Quran, firstMatch.Source);
        Assert.Equal("Qur'an 2:255", firstMatch.Reference);
        Assert.Equal(100.0, firstMatch.Score); // Capped at 100.0 max limit
        Assert.Contains("Exact reference match", firstMatch.Reasons);
    }

    [Fact]
    public async Task Search_AliasQuery_ReturnsAyatAlKursi()
    {
        var response = await _client.GetAsync("/api/v1/search?q=Ayat al Kursi");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EvidenceDossier>>();
        Assert.NotNull(result);
        
        var dossier = result.Data;
        Assert.Equal("ayat al kursi", dossier.ExecutionContext.OriginalQuery.ToLowerInvariant());
        
        var primary = dossier.Collections.FirstOrDefault(c => c.GroupName == "Primary Evidence")?.Items;
        Assert.NotNull(primary);
        Assert.NotEmpty(primary);
        
        var firstMatch = primary.First();
        Assert.Equal("Qur'an 2:255", firstMatch.Reference);
        Assert.Equal(97.0, firstMatch.Score); // 95 Alias match + 2.0 priority boost
        Assert.Contains("Alias reference match", firstMatch.Reasons);
    }

    [Fact]
    public async Task Search_KeywordQuery_ReturnsMatches()
    {
        var response = await _client.GetAsync("/api/v1/search?q=parents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EvidenceDossier>>();
        Assert.NotNull(result);

        var dossier = result.Data;
        var allItems = dossier.Collections.SelectMany(c => c.Items).ToList();
        Assert.NotEmpty(allItems);
        
        // Assert execution ID is tracked
        Assert.NotEqual(Guid.Empty, dossier.ExecutionContext.SearchId);
        Assert.NotNull(dossier.ExportMetadata);
    }

    [Fact]
    public async Task Search_ArabicQuery_ReturnsMatches()
    {
        var response = await _client.GetAsync("/api/v1/search?q=رحمن");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EvidenceDossier>>();
        Assert.NotNull(result);

        var dossier = result.Data;
        var allItems = dossier.Collections.SelectMany(c => c.Items).ToList();
        Assert.NotEmpty(allItems);
    }

    [Fact]
    public async Task GetByReference_ValidReference_ReturnsEvidenceItem()
    {
        var response = await _client.GetAsync("/api/v1/search/reference/2:255");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EvidenceItem>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        var item = result.Data;
        Assert.Equal(EvidenceSource.Quran, item.Source);
        Assert.Equal("Qur'an 2:255", item.Reference);
        Assert.Contains("لله", item.PrimaryText); // Cleaned arabic Ayat al Kursi
    }

    [Fact]
    public async Task GetSuggestions_ValidPrefix_ReturnsSuggestionsList()
    {
        var response = await _client.GetAsync("/api/v1/search/suggestions?q=Ay");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SearchSuggestionDto>>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        var suggestions = result.Data;
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Type == "Alias" && s.Value.Contains("Ayat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/search?q=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
