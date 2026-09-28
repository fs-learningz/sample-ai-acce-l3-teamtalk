using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TeamTalk.DataMigration;
using TeamTalk.Infrastructure.Persistence;

var sourceConnectionString = Environment.GetEnvironmentVariable("SOURCE_CONNECTION_STRING");
var targetConnectionString = Environment.GetEnvironmentVariable("TARGET_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(sourceConnectionString) || string.IsNullOrWhiteSpace(targetConnectionString))
{
    Console.Error.WriteLine("Set SOURCE_CONNECTION_STRING (SQL Server) and TARGET_CONNECTION_STRING (PostgreSQL) environment variables before running.");

    return 1;
}

var sourceOptions = new DbContextOptionsBuilder<TeamTalkDbContext>()
    .UseSqlServer(sourceConnectionString)
    .ReplaceService<IModelCacheKeyFactory, ProviderAwareModelCacheKeyFactory>()
    .Options;

var targetOptions = new DbContextOptionsBuilder<TeamTalkDbContext>()
    .UseNpgsql(targetConnectionString)
    .ReplaceService<IModelCacheKeyFactory, ProviderAwareModelCacheKeyFactory>()
    .Options;

await using var source = new TeamTalkDbContext(sourceOptions);
await using var target = new TeamTalkDbContext(targetOptions);

if (await target.Database.CanConnectAsync() is false)
{
    Console.Error.WriteLine("Cannot connect to the target PostgreSQL database. Check TARGET_CONNECTION_STRING.");

    return 1;
}

var appliedMigrations = (await target.Database.GetAppliedMigrationsAsync()).ToList();

if (appliedMigrations.Count is 0)
{
    Console.Error.WriteLine(
        "The target database has no applied migrations. Run 'dotnet ef database update' against " +
        "TARGET_CONNECTION_STRING (see this tool's README) before copying data.");

    return 1;
}

// Bulk insert-only workload: nothing here needs EF's change detection.
target.ChangeTracker.AutoDetectChangesEnabled = false;

var results = new List<(string Table, int SourceCount, int TargetCount)>();

// Copy order follows foreign key dependencies: every parent lands before its children.
results.Add(await CopyTableAsync(source, target, db => db.Roles, "AspNetRoles"));
results.Add(await CopyTableAsync(source, target, db => db.Users, "AspNetUsers"));
results.Add(await CopyTableAsync(source, target, db => db.RoleClaims, "AspNetRoleClaims"));
results.Add(await CopyTableAsync(source, target, db => db.UserClaims, "AspNetUserClaims"));
results.Add(await CopyTableAsync(source, target, db => db.UserLogins, "AspNetUserLogins"));
results.Add(await CopyTableAsync(source, target, db => db.UserRoles, "AspNetUserRoles"));
results.Add(await CopyTableAsync(source, target, db => db.UserTokens, "AspNetUserTokens"));
results.Add(await CopyTableAsync(source, target, db => db.Workspaces, "Workspaces"));
results.Add(await CopyTableAsync(source, target, db => db.WorkspaceMembers, "WorkspaceMembers"));
results.Add(await CopyTableAsync(source, target, db => db.WorkspaceInvitations, "WorkspaceInvitations"));
results.Add(await CopyTableAsync(source, target, db => db.Channels, "Channels"));
results.Add(await CopyTableAsync(source, target, db => db.ChannelMembers, "ChannelMembers"));
results.Add(await CopyTableAsync(source, target, db => db.Messages, "Messages"));

// AspNetUserClaims/AspNetRoleClaims use database-generated int identity columns. Postgres
// happily accepts the copied explicit Id values (IdentityByDefaultColumn allows that), but
// its sequence does not advance to match, so the next app-generated insert would collide
// with a copied row until the sequence is caught up.
await target.Database.ExecuteSqlAsync(
    $"SELECT setval(pg_get_serial_sequence('\"AspNetUserClaims\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"AspNetUserClaims\"), 1))");
await target.Database.ExecuteSqlAsync(
    $"SELECT setval(pg_get_serial_sequence('\"AspNetRoleClaims\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"AspNetRoleClaims\"), 1))");

Console.WriteLine();
Console.WriteLine("Row count verification:");
var mismatch = false;

foreach (var (table, sourceCount, targetCount) in results)
{
    var status = sourceCount == targetCount ? "OK" : "MISMATCH";
    mismatch |= sourceCount != targetCount;
    Console.WriteLine($"  {table,-22} source={sourceCount,-8} target={targetCount,-8} {status}");
}

await SpotCheckTimestampsAsync(source, target);

if (mismatch)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("One or more tables had a row count mismatch. Investigate before cutting over.");

    return 1;
}

// Foreign key integrity does not need a separate check here: the target schema's FK
// constraints would have rejected any orphaned row during the copy above, and the copy
// ordering guarantees parents exist before children are inserted.
Console.WriteLine();
Console.WriteLine("Copy complete. Verify the application against the target database, then delete this tool.");

return 0;

static async Task<(string Table, int SourceCount, int TargetCount)> CopyTableAsync<TEntity>(
    TeamTalkDbContext source,
    TeamTalkDbContext target,
    Func<TeamTalkDbContext, IQueryable<TEntity>> select,
    string tableName,
    int batchSize = 500)
    where TEntity : class
{
    Console.WriteLine($"Copying {tableName}...");

    var batch = new List<TEntity>(batchSize);
    var copied = 0;

    await foreach (var entity in select(source).AsNoTracking().AsAsyncEnumerable())
    {
        NormalizeUtcKind(entity);
        target.Add(entity);
        batch.Add(entity);
        copied++;

        if (batch.Count >= batchSize)
        {
            await target.SaveChangesAsync();
            target.ChangeTracker.Clear();
            batch.Clear();
        }
    }

    if (batch.Count > 0)
    {
        await target.SaveChangesAsync();
        target.ChangeTracker.Clear();
    }

    var targetCount = await select(target).CountAsync();
    Console.WriteLine($"  {tableName}: {copied} rows copied.");

    return (tableName, copied, targetCount);
}

// The SQL Server provider reads "datetime2" values back with Kind=Unspecified (SQL Server
// has no timezone-aware concept for that type). Npgsql's default "timestamp with time zone"
// mapping requires Kind=Utc on write and throws otherwise. Every timestamp in this model is
// UTC by convention (see the *Utc property naming), so re-tagging here is always correct.
//
// This goes through plain reflection on the CLR property rather than EF's PropertyEntry.
// CurrentValue: that setter round-trips the value through the property's value comparer,
// which for DateTime snapshots by ticks and silently drops Kind back to Unspecified — so by
// the time SaveChanges ran, the "fixed" value was unspecified again. Setting the property
// directly, before the entity is ever tracked, sidesteps that entirely. Domain entities use
// private setters, which PropertyInfo.SetValue can still assign.
static void NormalizeUtcKind<TEntity>(TEntity entity) where TEntity : class
{
    foreach (var property in typeof(TEntity).GetProperties())
    {
        if (property.PropertyType == typeof(DateTime))
        {
            var value = (DateTime)property.GetValue(entity)!;

            if (value.Kind is not DateTimeKind.Utc)
            {
                property.SetValue(entity, DateTime.SpecifyKind(value, DateTimeKind.Utc));
            }
        }
        else if (property.PropertyType == typeof(DateTime?))
        {
            if (property.GetValue(entity) is DateTime { Kind: not DateTimeKind.Utc } value)
            {
                property.SetValue(entity, DateTime.SpecifyKind(value, DateTimeKind.Utc));
            }
        }
    }
}

static async Task SpotCheckTimestampsAsync(TeamTalkDbContext source, TeamTalkDbContext target)
{
    if (await source.Messages.AnyAsync() is false)
    {
        return;
    }

    var sourceRange = await source.Messages.AsNoTracking()
        .GroupBy(_ => 1)
        .Select(g => new { Min = g.Min(m => m.SentAtUtc), Max = g.Max(m => m.SentAtUtc) })
        .SingleAsync();

    var targetRange = await target.Messages.AsNoTracking()
        .GroupBy(_ => 1)
        .Select(g => new { Min = g.Min(m => m.SentAtUtc), Max = g.Max(m => m.SentAtUtc) })
        .SingleAsync();

    Console.WriteLine();
    Console.WriteLine("Timestamp spot check (Messages.SentAtUtc):");
    Console.WriteLine($"  source min/max: {sourceRange.Min:O} / {sourceRange.Max:O}");
    Console.WriteLine($"  target min/max: {targetRange.Min:O} / {targetRange.Max:O}");
}
