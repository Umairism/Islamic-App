using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Evaluation;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Dossier;

public class DossierGenerator : IDossierGenerator
{
    private readonly string _storageRoot;

    public DossierGenerator()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _storageRoot = Path.Combine(baseDir, "storage", "dossiers");
    }

    public async Task<DossierGenerationResult> GenerateAsync(
        ResearchExecutionContext executionContext,
        EvaluationResult evaluation,
        CancellationToken cancellationToken = default)
    {
        var sessionId = executionContext.Session?.SessionId ?? Guid.NewGuid();
        var query = executionContext.Context?.Input?.Query?.OriginalRequest?.Query ?? "Islamic Research Query";
        var summary = executionContext.Reasoning?.Summary ?? "Synthesis complete.";
        var claims = executionContext.Reasoning?.Claims;
        var corpus = executionContext.Context?.Input?.Corpus;

        var sb = new StringBuilder();
        sb.AppendLine($"# Research Dossier");
        sb.AppendLine();
        sb.AppendLine($"**Question**: {query}");
        sb.AppendLine($"**Generated**: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Session ID**: `{sessionId}`");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 1. Executive Summary");
        sb.AppendLine();
        sb.AppendLine(summary);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 2. Verified Claims & Evidence Lineage");
        sb.AppendLine();

        if (claims != null && claims.Count > 0)
        {
            foreach (var claim in claims)
            {
                sb.AppendLine($"### Claim: {claim.Statement}");
                sb.AppendLine($"- **Confidence**: {Math.Round(claim.Confidence.Value * 100)}%");
                sb.AppendLine($"- **Supporting Evidence**: {string.Join(", ", claim.SupportingEvidence.Select(e => e.Value))}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 3. Primary Source Evidences");
        sb.AppendLine();

        if (corpus?.Evidences != null && corpus.Evidences.Count > 0)
        {
            foreach (var ev in corpus.Evidences)
            {
                sb.AppendLine($"#### [{ev.Source}] {ev.Title} (`Ref: {ev.Reference.Value}`)");
                sb.AppendLine($"> {ev.Content}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 4. Research Quality & Evaluation");
        sb.AppendLine();

        if (evaluation?.Score != null)
        {
            var s = evaluation.Score;
            sb.AppendLine($"| Quality Metric | Score |");
            sb.AppendLine($"| :--- | :--- |");
            sb.AppendLine($"| **Overall Research Score** | **{Math.Round(s.OverallScore * 100)}%** |");
            sb.AppendLine($"| Evidence Coverage | {Math.Round(s.EvidenceCoverage * 100)}% |");
            sb.AppendLine($"| Citation Accuracy | {Math.Round(s.CitationAccuracy * 100)}% |");
            sb.AppendLine($"| Reasoning Consistency | {Math.Round(s.ReasoningConsistency * 100)}% |");
            sb.AppendLine($"| Source Diversity | {Math.Round(s.SourceDiversity * 100)}% |");
            sb.AppendLine();
        }

        if (evaluation?.Findings != null && evaluation.Findings.Count > 0)
        {
            sb.AppendLine("### Evaluation Findings & Warnings");
            foreach (var f in evaluation.Findings)
            {
                sb.AppendLine($"- **[{f.Severity}]** `{f.Category}`: {f.Description}");
            }
            sb.AppendLine();
        }

        var markdown = sb.ToString();

        // Compute SHA-256 Hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(markdown));
        var contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // Save File to /storage/dossiers/{sessionId}/dossier.md
        var sessionDir = Path.Combine(_storageRoot, sessionId.ToString());
        Directory.CreateDirectory(sessionDir);
        var filePath = Path.Combine(sessionDir, "dossier.md");
        await File.WriteAllTextAsync(filePath, markdown, cancellationToken);

        return new DossierGenerationResult(
            MarkdownContent: markdown,
            ContentHash: contentHash,
            StoragePath: filePath
        );
    }
}
