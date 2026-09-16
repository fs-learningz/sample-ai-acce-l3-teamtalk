using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamTalk.Application.Common.Interfaces;
using Testcontainers.MsSql;

namespace TeamTalk.Api.IntegrationTests.Infrastructure;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-signing-key-please-do-not-use-in-prod-0123456789",
                ["Internal:ApiKey"] = "integration-test-internal-key",
                ["ConnectionStrings:teamtalkdb"] = _container.GetConnectionString(),
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMessageBroadcastService>();
            services.AddScoped<IMessageBroadcastService, NoOpMessageBroadcastService>();
        });
    }
}
