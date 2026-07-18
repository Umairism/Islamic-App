using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IslamicApp.Application.Research.Analysis;

public record SearchIndexItem(
    string EntityType,
    string EntityId,
    string Title,
    string Summary,
    string Content,
    DateTimeOffset OccurredAt
);

public interface ISearchIndex
{
    Task IndexAsync(SearchIndexItem item, CancellationToken cancellationToken);
    Task<IReadOnlyList<SearchIndexItem>> SearchAsync(string query, CancellationToken cancellationToken);
}
