using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IExportWriter
{
    string Format { get; }
    Task<RenderResult> WriteWorkspaceAsync(Workspace workspace, IEnumerable<ResearchDocument> documents, IEnumerable<ResearchNote> notes, CancellationToken cancellationToken);
}
