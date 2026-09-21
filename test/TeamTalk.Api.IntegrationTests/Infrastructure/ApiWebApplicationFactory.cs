using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamTalk.Application.Common.Interfaces;
using Testcontainers.MsSql;

namespace TeamTalk.Api.IntegrationTests.Infrastructure;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string JwtKeyEnvVar = "Jwt__Key";
    private const string InternalApiKeyEnvVar = "Internal__ApiKey";
    private const string ConnectionStringEnvVar = "ConnectionStrings__teamtalkdb";

    // Environment variables are process-wide, but each test class gets its own
    // ApiWebApplicationFactory (and its own container), and xUnit runs different test
    // classes in parallel by default. Without this lock, two factories initializing at the
    // same time can stomp on each other's env vars while their hosts are starting, so one
    // host ends up reading the other's (or nobody's) connection string.
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);

    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await InitializationLock.WaitAsync();
        try
        {
            // Program.cs and Aspire's client integrations (e.g. AddSqlServerDbContext) read some
            // configuration synchronously before WebApplicationFactory.ConfigureAppConfiguration's
            // callback runs, so those overrides arrive too late. Environment variables are picked up
            // by the default configuration providers the moment WebApplication.CreateBuilder runs,
            // which is early enough for both cases.
            Environment.SetEnvironmentVariable(JwtKeyEnvVar, "integration-test-signing-key-please-do-not-use-in-prod-0123456789");
            Environment.SetEnvironmentVariable(InternalApiKeyEnvVar, "integration-test-internal-key");
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, _container.GetConnectionString());

            // Force the host to build now, while the env vars above are guaranteed to still
            // reflect this factory's container, rather than lazily on the first request.
            _ = Server;
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    public new async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable(JwtKeyEnvVar, null);
        Environment.SetEnvironmentVariable(InternalApiKeyEnvVar, null);
        Environment.SetEnvironmentVariable(ConnectionStringEnvVar, null);

        await _container.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMessageBroadcastService>();
            services.AddScoped<IMessageBroadcastService, NoOpMessageBroadcastService>();
        });
    }
}
