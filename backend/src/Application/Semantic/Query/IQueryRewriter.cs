using System.Threading;
using System.Threading.Tasks;

namespace IslamicApp.Application.Semantic.Query;

public interface IQueryRewriter
{
    Task<SemanticQuery> RewriteAsync(string query, CancellationToken cancellationToken);
}
