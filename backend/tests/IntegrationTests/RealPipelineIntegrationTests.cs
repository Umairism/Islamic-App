using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Semantic.Query;
using IslamicApp.Infrastructure.Persistence;

namespace IslamicApp.IntegrationTests;

public class RealPipelineIntegrationTests : IClassFixture<WebApplicationFactory<WebApi.Program>>
{
    private readonly WebApplicationFactory<WebApi.Program> _factory;
    private readonly ITestOutputHelper _output;

    public RealPipelineIntegrationTests(WebApplicationFactory<WebApi.Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task ExecuteRealPipeline_CircumcisionQuery_AgainstPostgreSQL()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var dbContext = sp.GetRequiredService<ApplicationDbContext>();
        
        // Verify Postgres Connection & Ensure Tables
        bool canConnect = await dbContext.Database.CanConnectAsync();
        _output.WriteLine($"PostgreSQL Connection Status: {canConnect}");
        Assert.True(canConnect, "Could not connect to PostgreSQL Docker container on localhost:5432");

        await dbContext.Database.ExecuteSqlRawAsync(@"
            DROP TABLE IF EXISTS ""MemoryEntry"", ""ResearchSessions"", ""ResearchIterations"", ""ResearchEvents"", ""ResearchResults"", ""ResearchSession"", ""ResearchIteration"", ""ResearchEvent"", ""ResearchResult"";
            CREATE TABLE IF NOT EXISTS ""MemoryEntry"" (
                ""id"" UUID PRIMARY KEY,
                ""workspaceId"" UUID NOT NULL,
                ""query"" TEXT NOT NULL,
                ""summary"" TEXT NOT NULL,
                ""claimsJson"" TEXT NOT NULL,
                ""evidenceIdsJson"" TEXT NOT NULL,
                ""graphNodesJson"" TEXT NOT NULL,
                ""evidenceHash"" TEXT NOT NULL,
                ""methodology"" INT NOT NULL,
                ""confidenceEvidence"" DOUBLE PRECISION NOT NULL,
                ""confidenceCitation"" DOUBLE PRECISION NOT NULL,
                ""confidenceValidation"" DOUBLE PRECISION NOT NULL,
                ""confidenceReasoning"" DOUBLE PRECISION NOT NULL,
                ""confidenceMethodology"" DOUBLE PRECISION NOT NULL,
                ""createdAt"" TIMESTAMPTZ NOT NULL,
                ""schemaVersion"" INT NOT NULL,
                ""originSessionId"" UUID NOT NULL,
                ""originDocumentRevisionId"" UUID NOT NULL,
                ""compressedFromVersion"" INT NOT NULL,
                ""createdByModel"" TEXT NOT NULL,
                ""promptVersion"" TEXT NOT NULL,
                ""invalidated"" BOOLEAN NOT NULL,
                ""invalidationReason"" TEXT
            );
            CREATE TABLE IF NOT EXISTS ""ResearchSession"" (
                ""id"" UUID PRIMARY KEY,
                ""workspaceId"" UUID NOT NULL,
                ""title"" TEXT,
                ""query"" TEXT NOT NULL,
                ""status"" TEXT NOT NULL,
                ""currentStage"" TEXT,
                ""createdAt"" TIMESTAMPTZ NOT NULL,
                ""completedAt"" TIMESTAMPTZ,
                ""xmin"" BYTEA
            );
            CREATE TABLE IF NOT EXISTS ""ResearchIteration"" (
                ""id"" UUID PRIMARY KEY,
                ""researchSessionId"" UUID NOT NULL,
                ""iterationNumber"" INT NOT NULL,
                ""pipelineStage"" TEXT NOT NULL,
                ""confidenceScore"" DOUBLE PRECISION NOT NULL,
                ""gapsJson"" TEXT NOT NULL,
                ""retrievedNodesJson"" TEXT NOT NULL,
                ""newEvidenceJson"" TEXT NOT NULL,
                ""durationMs"" BIGINT NOT NULL,
                ""createdAt"" TIMESTAMPTZ NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ""ResearchEvent"" (
                ""id"" UUID PRIMARY KEY,
                ""researchSessionId"" UUID NOT NULL,
                ""eventType"" TEXT NOT NULL,
                ""payloadJson"" TEXT NOT NULL,
                ""createdAt"" TIMESTAMPTZ NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ""ResearchResult"" (
                ""id"" UUID PRIMARY KEY,
                ""researchSessionId"" UUID NOT NULL,
                ""answerText"" TEXT NOT NULL,
                ""confidenceScore"" DOUBLE PRECISION NOT NULL,
                ""citationsJson"" TEXT NOT NULL,
                ""version"" INT NOT NULL,
                ""isFinal"" BOOLEAN NOT NULL,
                ""generatedAt"" TIMESTAMPTZ NOT NULL
            );
        ");

        var queryAnalyzer = sp.GetRequiredService<IQueryAnalyzer>();
        var queryRewriter = sp.GetRequiredService<IQueryRewriter>();
        var pipeline = sp.GetRequiredService<IResearchPipeline>();

        // Construct Real Query Request
        var queryText = "What are the ruling about circumcision in Islam?";
        var searchReq = new SearchRequest(
            Query: queryText,
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(1, 20),
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: true
        );

        var queryAnalysis = await queryAnalyzer.AnalyzeAsync(searchReq);
        var rewrittenQuery = await queryRewriter.RewriteAsync(queryText, CancellationToken.None);
        queryAnalysis = queryAnalysis with { SemanticQuery = rewrittenQuery };

        _output.WriteLine($"Executing Real Research Pipeline for Query: '{queryText}'");

        // Execute Pipeline
        var result = await pipeline.ExecuteAsync(queryAnalysis, cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess, $"Pipeline execution failed: {result.Error?.Message}");

        var execCtx = result.Value;
        Assert.NotNull(execCtx);
        Assert.Equal(PipelineStage.Completed, execCtx.CurrentStage);

        _output.WriteLine("\n=================== REAL RESEARCH RESULT SUMMARY ===================");
        _output.WriteLine(execCtx.Reasoning?.Summary ?? "No summary produced");

        _output.WriteLine("\n=================== REAL CLAIMS & EVIDENCE ===================");
        if (execCtx.Reasoning?.Claims != null)
        {
            foreach (var claim in execCtx.Reasoning.Claims)
            {
                _output.WriteLine($"- Claim: {claim.Statement} (Confidence: {claim.Confidence.Value})");
            }
        }

        _output.WriteLine("\n=================== REAL RENDERED MARKDOWN OUTPUT ===================");
        var markdownOutput = execCtx.RenderedOutputs?.FirstOrDefault(r => r.Extension == ".md")?.Content;
        _output.WriteLine(markdownOutput ?? "No markdown rendered");
    }
}
