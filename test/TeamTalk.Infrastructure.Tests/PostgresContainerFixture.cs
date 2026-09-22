using Microsoft.EntityFrameworkCore;
using TeamTalk.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace TeamTalk.Infrastructure.Tests;

public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

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
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new TeamTalkDbContext(options);
    }
}
