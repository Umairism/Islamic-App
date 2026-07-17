using System.Collections.Concurrent;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Cache;

public class EmbeddingCache : IEmbeddingCache
{
    private readonly ConcurrentDictionary<string, float[]> _cache = new();

    public bool TryGet(string text, ResearchLanguage language, out float[] vector)
    {
        string key = GetKey(text, language);
        return _cache.TryGetValue(key, out vector!);
    }

    public void Store(string text, ResearchLanguage language, float[] vector)
    {
        if (vector == null) return;
        string key = GetKey(text, language);
        _cache[key] = vector;
    }

    private string GetKey(string text, ResearchLanguage language)
    {
        return $"{language}:{text.Trim().ToLowerInvariant()}";
    }
}
