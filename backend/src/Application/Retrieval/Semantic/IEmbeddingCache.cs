using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Retrieval.Semantic;

public interface IEmbeddingCache
{
    bool TryGet(string text, ResearchLanguage language, out float[] vector);
    void Store(string text, ResearchLanguage language, float[] vector);
}
