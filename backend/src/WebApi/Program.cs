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
}
