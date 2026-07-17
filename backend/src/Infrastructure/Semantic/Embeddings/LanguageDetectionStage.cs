using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Embeddings;

public class LanguageDetectionStage : IEmbeddingPipelineStage
{
    public Task<EmbeddingPipelineContext> ExecuteAsync(
        EmbeddingPipelineContext context,
        CancellationToken cancellationToken)
    {
        var language = context.Request.Language;
        
        if (language == ResearchLanguage.Auto)
        {
            bool hasArabic = false;
            string text = context.Request.Text;
            for (int i = 0; i < text.Length; i++)
            {
                // Check for Arabic unicode block
                if (text[i] >= 0x0600 && text[i] <= 0x06FF)
                {
                    hasArabic = true;
                    break;
                }
            }
            
            language = hasArabic ? ResearchLanguage.Arabic : ResearchLanguage.English;
        }

        var updated = context with
        {
            Request = context.Request with { Language = language },
            ProcessingHistory = context.ProcessingHistory.Add($"LanguageDetectionStage: {language}")
        };

        return Task.FromResult(updated);
    }
}
