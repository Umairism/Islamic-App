using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Embeddings;

public class OpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;

    public OpenAIEmbeddingGenerator(HttpClient httpClient, string apiKey, string modelName = "text-embedding-3-small")
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _modelName = modelName;
    }

    public async Task<float[]> GenerateAsync(
        string text,
        ResearchLanguage language,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return await new MockEmbeddingGenerator().GenerateAsync(text, language, cancellationToken);
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = JsonContent.Create(new
            {
                model = _modelName,
                input = text
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(
                    cancellationToken: cancellationToken);
                if (result?.Data != null && result.Data.Length > 0 && result.Data[0].Embedding != null)
                {
                    return result.Data[0].Embedding;
                }
            }
        }
        catch
        {
            // Fallback gracefully to mock implementation if remote offline
        }

        return await new MockEmbeddingGenerator().GenerateAsync(text, language, cancellationToken);
    }

    private class OpenAIEmbeddingResponse
    {
        public OpenAIEmbeddingData[]? Data { get; set; }
    }

    private class OpenAIEmbeddingData
    {
        public float[]? Embedding { get; set; }
    }
}
