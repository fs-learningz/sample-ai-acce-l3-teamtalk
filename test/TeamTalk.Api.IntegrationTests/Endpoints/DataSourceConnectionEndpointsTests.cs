using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using TeamTalk.Infrastructure.Persistence;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class DataSourceConnectionEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public DataSourceConnectionEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ConnectionString_ForTeamTalkDb_IsConfiguredAndParsable()
    {
        // arrange
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // act
        var connectionString = configuration.GetConnectionString("teamtalkdb");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        // assert
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        Assert.False(string.IsNullOrWhiteSpace(builder.Host));
        Assert.True(builder.Port > 0);
        Assert.False(string.IsNullOrWhiteSpace(builder.Database));
    }

    [Fact]
    public async Task DbContext_ResolvedFromApplicationHost_CanConnectToDataSource()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamTalkDbContext>();

        // act
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        // assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DbContext_ResolvedFromApplicationHost_HasNoPendingMigrations()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamTalkDbContext>();

        // act
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

        // assert
        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task ConnectionString_ForTeamTalkDb_OpensConnectionAndExecutesQuery()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("teamtalkdb"));

        // act
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);

        // assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        Assert.Equal(1, scalar);
    }
}
