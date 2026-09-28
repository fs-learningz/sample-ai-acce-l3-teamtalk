using TeamTalk.AppHost;

ContainerRuntimePreflight.EnsureRuntimeIsRunning();

var builder = DistributedApplication.CreateBuilder(args);

var jwtKey = builder.AddParameter("jwt-key", secret: true);
var internalApiKey = builder.AddParameter("internal-api-key", secret: true);

var postgresDb = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume()
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050))
    .WithPgWeb(pgWeb => pgWeb.WithHostPort(8081))
    .AddDatabase("teamtalkdb");

var hub = builder.AddProject<Projects.TeamTalk_Hub>("hub")
    .WithReference(postgresDb)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Internal__ApiKey", internalApiKey)
    .WithHttpEndpoint(port: 5090, name: "http")
    .WaitFor(postgresDb);

var api = builder.AddProject<Projects.TeamTalk_Api>("api")
    .WithReference(postgresDb)
    .WithReference(hub)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Internal__ApiKey", internalApiKey)
    .WithHttpEndpoint(port: 5080, name: "http")
    .WaitFor(postgresDb);

builder.AddViteApp("site", "../TeamTalk.Site", "start")
    .WithReference(api)
    .WithReference(hub)
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
