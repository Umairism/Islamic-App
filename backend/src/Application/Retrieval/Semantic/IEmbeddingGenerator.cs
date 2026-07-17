using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Retrieval.Semantic;

public interface IEmbeddingGenerator
{
    Task<float[]> GenerateAsync(
        string text,
        ResearchLanguage language,
        CancellationToken cancellationToken);
}
