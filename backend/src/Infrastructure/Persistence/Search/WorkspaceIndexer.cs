using System.Threading;
using System.Threading.Tasks;
using MediatR;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;

namespace IslamicApp.Infrastructure.Persistence.Search;

public class WorkspaceIndexer : 
    INotificationHandler<ResearchPublishedEvent>,
    INotificationHandler<NoteCreatedEvent>
{
    private readonly ISearchIndex _searchIndex;

    public WorkspaceIndexer(ISearchIndex searchIndex)
    {
        _searchIndex = searchIndex;
    }

    public async Task Handle(ResearchPublishedEvent notification, CancellationToken cancellationToken)
    {
        var item = new SearchIndexItem(
            EntityType: "DocumentRevision",
            EntityId: notification.RevisionId.ToString(),
            Title: $"Research Document Revision: {notification.DocumentId}",
            Summary: notification.Summary,
            Content: notification.Summary,
            OccurredAt: notification.OccurredAt
        );

        await _searchIndex.IndexAsync(item, cancellationToken);
    }

    public async Task Handle(NoteCreatedEvent notification, CancellationToken cancellationToken)
    {
        var item = new SearchIndexItem(
            EntityType: "Note",
            EntityId: notification.NoteId.ToString(),
            Title: notification.Title,
            Summary: string.Empty,
            Content: $"Note: {notification.Title}",
            OccurredAt: notification.OccurredAt
        );

        await _searchIndex.IndexAsync(item, cancellationToken);
    }
}
