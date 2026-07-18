using System;

namespace IslamicApp.Application.Research.Analysis;

public interface IBackgroundJobScheduler
{
    Guid EnqueueExportJob(Guid workspaceId, string format);
}
