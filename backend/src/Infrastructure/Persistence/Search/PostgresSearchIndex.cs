using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Persistence.Search;

public class PostgresSearchIndex : ISearchIndex
{
    private readonly ApplicationDbContext _dbContext;

    public PostgresSearchIndex(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task IndexAsync(SearchIndexItem item, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SearchIndices.FindAsync(new object[] { item.EntityId }, cancellationToken);
        if (existing != null)
        {
            existing.Title = item.Title;
            existing.Summary = item.Summary;
            existing.Content = item.Content;
            existing.OccurredAt = item.OccurredAt;
        }
        else
        {
            var entity = new SearchIndexEntity
            {
                EntityId = item.EntityId,
                EntityType = item.EntityType,
                Title = item.Title,
                Summary = item.Summary,
                Content = item.Content,
                OccurredAt = item.OccurredAt
            };
            _dbContext.SearchIndices.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchIndexItem>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query.ToLowerInvariant();
        var matches = await _dbContext.SearchIndices
            .Where(s => s.Title.ToLower().Contains(normalizedQuery) ||
                        s.Summary.ToLower().Contains(normalizedQuery) ||
                        s.Content.ToLower().Contains(normalizedQuery))
            .Select(s => new SearchIndexItem(s.EntityType, s.EntityId, s.Title, s.Summary, s.Content, s.OccurredAt))
            .ToListAsync(cancellationToken);

        return matches;
    }
}
