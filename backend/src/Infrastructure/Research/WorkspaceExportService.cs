using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;

namespace IslamicApp.Infrastructure.Research;

public class WorkspaceExportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<IExportWriter> _writers;
    private readonly ILogger<WorkspaceExportService> _logger;

    public WorkspaceExportService(
        ApplicationDbContext dbContext,
        IEnumerable<IExportWriter> writers,
        ILogger<WorkspaceExportService> logger)
    {
        _dbContext = dbContext;
        _writers = writers;
        _logger = logger;
    }

    public async Task<RenderResult> ExecuteExportAsync(Guid workspaceId, string format, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Exporting workspace {WorkspaceId} to format {Format}...", workspaceId, format);

        var workspaceEntity = await _dbContext.Workspaces
            .Include(w => w.Sessions)
            .Include(w => w.Notes)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspaceEntity == null)
        {
            throw new ArgumentException($"Workspace {workspaceId} not found.");
        }

        var workspace = new Workspace(workspaceEntity.Id, workspaceEntity.Name, workspaceEntity.Description, workspaceEntity.CreatedAt);

        // Fetch documents
        var documentEntities = await _dbContext.ResearchDocuments
            .Where(d => d.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        var documents = documentEntities.Select(d => new ResearchDocument(d.Id, d.SessionId, d.Title, d.WorkspaceId, d.CurrentRevisionId, d.CreatedAt)).ToList();

        // Fetch notes
        var notes = workspaceEntity.Notes.Select(n => new ResearchNote(n.Id, n.WorkspaceId, n.Title, n.Markdown, n.CreatedAt, n.UpdatedAt)).ToList();

        var writer = _writers.FirstOrDefault(w => w.Format.Equals(format, StringComparison.OrdinalIgnoreCase));
        if (writer == null)
        {
            throw new NotSupportedException($"Format '{format}' is not supported by the export writers.");
        }

        var result = await writer.WriteWorkspaceAsync(workspace, documents, notes, cancellationToken);
        _logger.LogInformation("Successfully completed workspace export {WorkspaceId} in {Format}", workspaceId, format);

        return result;
    }
}
