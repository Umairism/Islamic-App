using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.Services;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Repositories;
using IslamicApp.WebApi.Middleware;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Infrastructure.Search;
using IslamicApp.Application.Research.Catalog;
using IslamicApp.Infrastructure.Search.Citation;
using IslamicApp.Infrastructure.Search.CrossReference;
using IslamicApp.Infrastructure.Search.Export;
using IslamicApp.Infrastructure.Search.Plugins;
using IslamicApp.Application.Semantic.Query;
using IslamicApp.Application.Semantic.Reasoning;
using IslamicApp.Application.Retrieval.Semantic;
using IslamicApp.Application.Retrieval.Hybrid;
using IslamicApp.Application.Retrieval.Benchmark;
using IslamicApp.Application.Retrieval.Lexical;
using IslamicApp.Infrastructure.Retrieval.Lexical;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Infrastructure.Research.Analysis;
using IslamicApp.Infrastructure.Research.Analysis.Methodologies;
using IslamicApp.Infrastructure.Research.Analysis.ConflictRules;
using IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;
using IslamicApp.Infrastructure.Semantic.Query;
using IslamicApp.Infrastructure.Semantic.Embeddings;
using IslamicApp.Infrastructure.Semantic.Vector;
using IslamicApp.Infrastructure.Semantic.Cache;
using IslamicApp.Infrastructure.Semantic.Reasoning;
using IslamicApp.Infrastructure.Retrieval.Semantic;
using IslamicApp.Infrastructure.Retrieval.Hybrid;
using IslamicApp.Infrastructure.Retrieval.Benchmark;

namespace IslamicApp.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        // Load Env file variables
        LoadEnv();

        var builder = WebApplication.CreateBuilder(args);

        // Setup Logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();

        // Get DB connection string from Environment or fallback
        var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            rawConnectionString = "postgresql://postgres:password123@localhost:5432/islamic_research?schema=public";
        }

        var connectionString = ConvertPrismaConnectionStringToNpgsql(rawConnectionString);

        // Configure Context and services
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.AddScoped<ISurahRepository, SurahRepository>();
        builder.Services.AddScoped<IVerseRepository, VerseRepository>();
        builder.Services.AddScoped<ITranslationRepository, TranslationRepository>();
        builder.Services.AddScoped<IDatasetRepository, DatasetRepository>();
        builder.Services.AddScoped<IImportSessionRepository, ImportSessionRepository>();

        builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
        builder.Services.AddScoped<IEvidenceService, EvidenceService>();
        builder.Services.AddScoped<IHealthService, HealthService>();

        // Register Milestone 6A refactored search configurations and dynamic sources
        var configProvider = new SearchConfigurationProvider();
        builder.Services.AddSingleton<ISynonymProvider>(configProvider);
        builder.Services.AddSingleton<IAliasProvider>(configProvider);
        builder.Services.AddSingleton<IStopWordProvider>(configProvider);
        builder.Services.AddSingleton<IRankingWeightsProvider>(configProvider);

        builder.Services.AddSingleton<ISearchNormalizer, SearchNormalizer>();
        builder.Services.AddSingleton<ITokenizer, Tokenizer>();
        builder.Services.AddSingleton<ISourceReferenceResolver, SourceReferenceResolver>();
        builder.Services.AddSingleton<ISynonymEngine, SynonymEngine>();
        builder.Services.AddSingleton<IHighlightBuilder, HighlightBuilder>();
        builder.Services.AddSingleton<SuggestionIndex>();

        // Dynamic capability searchers and citations strategies
        builder.Services.AddScoped<QuranSearcher>();
        builder.Services.AddScoped<HadithSearcher>();
        builder.Services.AddScoped<ISourceSearcher>(sp => sp.GetRequiredService<QuranSearcher>());
        builder.Services.AddScoped<ISourceSearcher>(sp => sp.GetRequiredService<HadithSearcher>());
        builder.Services.AddSingleton<QuranCitationStrategy>();
        builder.Services.AddSingleton<HadithCitationStrategy>();
        builder.Services.AddSingleton<ICitationStrategy>(sp => sp.GetRequiredService<QuranCitationStrategy>());
        builder.Services.AddSingleton<ICitationStrategy>(sp => sp.GetRequiredService<HadithCitationStrategy>());

        builder.Services.AddScoped<ICitationFormatter, CitationFormatter>();
        builder.Services.AddScoped<KnowledgeCatalog>();

        // Dynamic knowledge sources (plugins)
        builder.Services.AddScoped<IKnowledgeSource, QuranSource>();
        builder.Services.AddScoped<IKnowledgeSource, HadithSource>();

        builder.Services.AddScoped<IQueryAnalyzer, QueryAnalyzer>();
        builder.Services.AddScoped<ILexicalRetriever, LexicalRetriever>();
        
        // Milestone 6B registrations
        builder.Services.AddSingleton<ISemanticConfiguration, SearchConfigurationProvider>();
        builder.Services.AddSingleton<ISimilarityMetric, CosineSimilarity>();
        builder.Services.AddSingleton<IEmbeddingGenerator, MockEmbeddingGenerator>();
        builder.Services.AddSingleton<IEmbeddingCache, EmbeddingCache>();
        builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        builder.Services.AddSingleton<IFusionStrategy, ReciprocalRankFusion>();
        builder.Services.AddScoped<IQueryRewriter, QueryRewriter>();
        builder.Services.AddScoped<ISemanticRetriever, SemanticRetriever>();
        builder.Services.AddScoped<IRetrievalOrchestrator, RetrievalOrchestrator>();
        builder.Services.AddScoped<IKnowledgeReasoner, NotImplementedReasoner>();
        builder.Services.AddScoped<ISemanticBenchmarkRunner, SemanticBenchmarkRunner>();

        // Milestone 7A Research Core Registrations
        builder.Services.AddScoped<IEvidenceRepository, EvidenceRepository>();
        builder.Services.AddScoped<IEvidenceDeduplicator, EvidenceDeduplicator>();
        builder.Services.AddScoped<IEvidenceAnalyzer, EvidenceAnalyzer>();
        builder.Services.AddScoped<IGraphBuilder, GraphBuilder>();
        builder.Services.AddScoped<IConflictDetector, ConflictDetector>();
        builder.Services.AddScoped<IMethodologySelector, MethodologySelector>();
        builder.Services.AddScoped<IResearchMethodologyFactory, ResearchMethodologyFactory>();
        builder.Services.AddScoped<IResearchAnalysisBuilder, ResearchAnalysisBuilder>();
        builder.Services.AddScoped<IResearchPipeline, ResearchPipeline>();

        // Pluggable Methodologies
        builder.Services.AddScoped<IResearchMethodology, ThematicMethodology>();
        builder.Services.AddScoped<IResearchMethodology, LiteralMethodology>();
        builder.Services.AddScoped<IResearchMethodology, ComparativeMethodology>();
        builder.Services.AddScoped<IResearchMethodology, FiqhMethodology>();
        builder.Services.AddScoped<IResearchMethodology, AqidahMethodology>();
        builder.Services.AddScoped<IResearchMethodology, LinguisticMethodology>();
        builder.Services.AddScoped<IResearchMethodology, HistoricalMethodology>();
        builder.Services.AddScoped<IResearchMethodology, ChronologicalMethodology>();
        builder.Services.AddScoped<IResearchMethodology, TafsirMethodology>();

        // Pluggable Conflict Rules
        builder.Services.AddScoped<IConflictRule, WeakNarrationRule>();
        builder.Services.AddScoped<IConflictRule, AbrogationRule>();
        builder.Services.AddScoped<IConflictRule, MadhhabDifferenceRule>();
        builder.Services.AddScoped<IConflictRule, ContextDifferenceRule>();

        // Pluggable Pipeline Behaviors
        builder.Services.AddScoped<IResearchPipelineBehavior, ExceptionBehavior>();
        builder.Services.AddScoped<IResearchPipelineBehavior, MetricsBehavior>();
        builder.Services.AddScoped<IResearchPipelineBehavior, LoggingBehavior>();
        builder.Services.AddScoped<IResearchPipelineBehavior, RetrievalBehavior>();
        builder.Services.AddScoped<IResearchPipelineBehavior, DeduplicationBehavior>();
        builder.Services.AddScoped<IResearchPipelineBehavior, AnalysisBehavior>();

        // Embedding Pipeline Stages
        builder.Services.AddScoped<IEmbeddingPipelineStage, NormalizationStage>();
        builder.Services.AddScoped<IEmbeddingPipelineStage, LanguageDetectionStage>();
        builder.Services.AddScoped<IEmbeddingPipelineStage, EmbeddingGenerationStage>();
        builder.Services.AddScoped<IEmbeddingPipeline, EmbeddingPipeline>();

        builder.Services.AddScoped<IRankingEngine, RankingEngine>();
        builder.Services.AddScoped<IEvidenceBuilder, EvidenceBuilder>();
        builder.Services.AddScoped<ISearchPipeline, SearchPipeline>();
        builder.Services.AddScoped<IResearchService, ResearchService>();

        // Register Milestone 5 dynamic Cross-Reference and OCP Export Formatters
        builder.Services.AddScoped<QuranCrossReferenceProvider>();
        builder.Services.AddScoped<HadithCrossReferenceProvider>();
        builder.Services.AddScoped<ICrossReferenceProvider>(sp => sp.GetRequiredService<QuranCrossReferenceProvider>());
        builder.Services.AddScoped<ICrossReferenceProvider>(sp => sp.GetRequiredService<HadithCrossReferenceProvider>());
        builder.Services.AddScoped<ICrossReferenceEngine, CrossReferenceEngine>();

        builder.Services.AddScoped<IExportFormatter, JsonExportFormatter>();
        builder.Services.AddScoped<IExportFormatter, MarkdownExportFormatter>();
        builder.Services.AddScoped<IExportFormatter, HtmlExportFormatter>();
        builder.Services.AddScoped<IExportEngine, ExportEngine>();

        // Register Pipeline Stages
        builder.Services.AddScoped<DatabaseQueryStage>();
        builder.Services.AddScoped<RankingStage>();
        builder.Services.AddScoped<EvidenceBuildStage>();

        builder.Services.AddControllers();

        // Setup API Versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
        });

        builder.Services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Setup Swagger
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Islamic Research Platform API",
                Version = "v1",
                Description = "Read-Only REST API for retrieves Quranic resources."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        var app = builder.Build();

        // 1. Startup health check verify search configurations exist
        VerifySearchConfigurationsExist();

        // 2. Pre-initialize suggestion trie index in-memory
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var index = app.Services.GetRequiredService<SuggestionIndex>();
            index.InitializeAsync(db).GetAwaiter().GetResult();
        }

        // Register custom middlewares
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Islamic Research Platform API v1");
            });
        }

        app.MapControllers();

        try
        {
            Log.Information("Starting Web Host...");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static string ConvertPrismaConnectionStringToNpgsql(string prismaUrl)
    {
        if (string.IsNullOrWhiteSpace(prismaUrl)) return string.Empty;
        if (!prismaUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return prismaUrl; // Already in standard format
        }

        try
        {
            var cleanUrl = prismaUrl.Substring("postgresql://".Length);
            var atIndex = cleanUrl.IndexOf('@');
            if (atIndex == -1) return prismaUrl;

            var userInfo = cleanUrl.Substring(0, atIndex);
            var hostDbInfo = cleanUrl.Substring(atIndex + 1);

            var colonIndex = userInfo.IndexOf(':');
            var username = colonIndex == -1 ? userInfo : userInfo.Substring(0, colonIndex);
            var password = colonIndex == -1 ? string.Empty : userInfo.Substring(colonIndex + 1);

            var slashIndex = hostDbInfo.IndexOf('/');
            if (slashIndex == -1) return prismaUrl;

            var hostPort = hostDbInfo.Substring(0, slashIndex);
            var dbOptions = hostDbInfo.Substring(slashIndex + 1);

            var hostPortColon = hostPort.IndexOf(':');
            var host = hostPortColon == -1 ? hostPort : hostPort.Substring(0, hostPortColon);
            var port = hostPortColon == -1 ? "5432" : hostPort.Substring(hostPortColon + 1);

            var questionIndex = dbOptions.IndexOf('?');
            var database = questionIndex == -1 ? dbOptions : dbOptions.Substring(0, questionIndex);

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        }
        catch
        {
            return prismaUrl;
        }
    }

    private static void LoadEnv()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var etlEnv = Path.Combine(current, "etl", ".env");
            if (File.Exists(etlEnv))
            {
                LoadEnvFile(etlEnv);
                return;
            }
            var rootEnv = Path.Combine(current, ".env");
            if (File.Exists(rootEnv))
            {
                LoadEnvFile(rootEnv);
                return;
            }
            var parent = Directory.GetParent(current);
            if (parent == null || parent.FullName == current) break;
            current = parent.FullName;
        }
    }

    private static void LoadEnvFile(string path)
    {
        try
        {
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                var key = parts[0].Trim();
                var val = parts[1].Trim().Trim('"').Trim('\'');
                Environment.SetEnvironmentVariable(key, val);
            }
        }
        catch
        {
            // Ignore error
        }
    }

    private static void VerifySearchConfigurationsExist()
    {
        var requiredFiles = new[] { "aliases.json", "synonyms.json", "ranking.json", "stopwords.json", "surah-names.json" };
        foreach (var file in requiredFiles)
        {
            try
            {
                FindConfigPath(file);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Critical startup failure: Search configuration file {file} is missing.", ex);
            }
        }
    }

    private static string FindConfigPath(string fileName)
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var configDir = Path.Combine(current, "Configuration", "Search");
            if (Directory.Exists(configDir))
            {
                var filePath = Path.Combine(configDir, fileName);
                if (File.Exists(filePath))
                    return filePath;
            }

            var rootConfigDir = Path.Combine(current, "backend", "Configuration", "Search");
            if (Directory.Exists(rootConfigDir))
            {
                var filePath = Path.Combine(rootConfigDir, fileName);
                if (File.Exists(filePath))
                    return filePath;
            }

            var parent = Directory.GetParent(current);
            if (parent == null || parent.FullName == current) break;
            current = parent.FullName;
        }
        throw new FileNotFoundException($"Configuration file {fileName} not found.");
    }
}
