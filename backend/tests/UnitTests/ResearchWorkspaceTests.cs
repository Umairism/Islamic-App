using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Entities;
using IslamicApp.Infrastructure.Persistence.Outbox;
using IslamicApp.Infrastructure.Persistence.Search;
using IslamicApp.Infrastructure.Research.Export;
using IslamicApp.Infrastructure.Research;

namespace IslamicApp.UnitTests;

public class ResearchWorkspaceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task OutboxService_Should_Save_Message_To_Database()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var outboxService = new OutboxService(dbContext);
        var startedEvent = new ResearchStartedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            SessionId: Guid.NewGuid(),
            Query: "test query",
            WorkspaceId: Guid.NewGuid()
        );

        // Action
        await outboxService.WriteEventAsync(startedEvent, CancellationToken.None);

        // Assert
        var message = await dbContext.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(message);
        Assert.Contains("ResearchStartedEvent", message.EventType);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public async Task PostgresSearchIndex_Should_Index_And_Search_Correctly()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var searchIndex = new PostgresSearchIndex(dbContext);
        var item = new SearchIndexItem(
            EntityType: "Note",
            EntityId: "note-1",
            Title: "Ibn Kathir notes",
            Summary: "Comparing opinions",
            Content: "Note contents on Surah Al-Baqarah",
            OccurredAt: DateTimeOffset.UtcNow
        );

        // Action
        await searchIndex.IndexAsync(item, CancellationToken.None);

        // Assert
        var searchResults = await searchIndex.SearchAsync("Kathir", CancellationToken.None);
        Assert.Single(searchResults);
        Assert.Equal("Ibn Kathir notes", searchResults[0].Title);
    }

    [Fact]
    public async Task ExportWriters_Should_Format_Workspace_Correctly()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "My Quranic Research", "Deep study on prayer", DateTimeOffset.UtcNow);
        var documents = new List<ResearchDocument>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Salah in Quran", workspace.Id, null, DateTimeOffset.UtcNow)
        };
        var notes = new List<ResearchNote>
        {
            new(Guid.NewGuid(), workspace.Id, "Tafsir notes", "Ibn Kathir says prayer is core.", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        };

        var markdownWriter = new MarkdownWorkspaceWriter();
        var htmlWriter = new HtmlWorkspaceWriter();
        var jsonWriter = new JsonWorkspaceWriter();

        // Action
        var mdResult = await markdownWriter.WriteWorkspaceAsync(workspace, documents, notes, CancellationToken.None);
        var htmlResult = await htmlWriter.WriteWorkspaceAsync(workspace, documents, notes, CancellationToken.None);
        var jsonResult = await jsonWriter.WriteWorkspaceAsync(workspace, documents, notes, CancellationToken.None);

        // Assert
        Assert.Contains("# Workspace Export: My Quranic Research", mdResult.Content);
        Assert.Contains("<html>", htmlResult.Content);
        Assert.Contains("Tafsir notes", jsonResult.Content);
    }
}
