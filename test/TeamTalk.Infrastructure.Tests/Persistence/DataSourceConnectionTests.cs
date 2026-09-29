using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeamTalk.Infrastructure.Persistence;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class DataSourceConnectionTests
{
    private readonly PostgresContainerFixture _fixture;

    public DataSourceConnectionTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ConnectionString_FromFixture_IsValidAndContainsRequiredParts()
    {
        // act
        var builder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString);

        // assert
        Assert.False(string.IsNullOrWhiteSpace(builder.Host));
        Assert.True(builder.Port > 0);
        Assert.False(string.IsNullOrWhiteSpace(builder.Database));
        Assert.False(string.IsNullOrWhiteSpace(builder.Username));
    }

    [Theory]
    [InlineData("Host=localhost;Port=not-a-number")]
    [InlineData("this is not a connection string")]
    public void ConnectionString_WhenMalformed_ThrowsArgumentException(string connectionString)
    {
        // act & assert
        Assert.Throws<ArgumentException>(() => new NpgsqlConnectionStringBuilder(connectionString));
    }

    [Fact]
    public async Task OpenAsync_WithValidConnectionString_EstablishesConnection()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);

        // act
        await connection.OpenAsync(cancellationToken);

        // assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task ExecuteScalarAsync_OnOpenConnection_ReturnsQueryResult()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT 1", connection);

        // act
        var scalar = await command.ExecuteScalarAsync(cancellationToken);

        // assert
        Assert.Equal(1, scalar);
    }

    [Fact]
    public async Task CanConnectAsync_WithValidConnectionString_ReturnsTrue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = _fixture.CreateDbContext();

        // act
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        // assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task GetPendingMigrationsAsync_AfterFixtureInitialization_ReturnsEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = _fixture.CreateDbContext();

        // act
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

        // assert
        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task CanConnectAsync_WhenHostIsUnreachable_ReturnsFalse()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Port = 1,
            Timeout = 2,
            CommandTimeout = 2,
        };
        await using var dbContext = CreateDbContext(builder.ConnectionString);

        // act
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        // assert
        Assert.False(canConnect);
    }

    [Fact]
    public async Task OpenAsync_WhenPasswordIsWrong_ThrowsPostgresException()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Password = "definitely-the-wrong-password",
            Pooling = false,
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.OpenAsync(cancellationToken));

        // assert
        Assert.Equal("28P01", exception.SqlState);
    }

    [Fact]
    public async Task OpenAsync_WhenDatabaseDoesNotExist_ThrowsPostgresException()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Database = $"missing_{Guid.NewGuid():N}",
            Pooling = false,
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.OpenAsync(cancellationToken));

        // assert
        Assert.Equal("3D000", exception.SqlState);
    }

    private static TeamTalkDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TeamTalkDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TeamTalkDbContext(options);
    }
}
