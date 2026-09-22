using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TeamTalk.DataMigration;

/// <summary>
/// EF Core's default IModelCacheKeyFactory keys the cached model only by DbContext type,
/// which assumes one DbContext type maps to exactly one provider. This tool constructs
/// TeamTalkDbContext against two providers (SqlServer source, Npgsql target) in the same
/// process, so without this override the second provider would silently reuse a model built
/// for the first one. Keying by provider name as well keeps the two models separate.
/// </summary>
public class ProviderAwareModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => (context.GetType(), context.Database.ProviderName, designTime);
}
