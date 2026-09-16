using Microsoft.EntityFrameworkCore;
using TeamTalk.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace TeamTalk.Infrastructure.Tests;

public class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public TeamTalkDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TeamTalkDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        return new TeamTalkDbContext(options);
    }
}
