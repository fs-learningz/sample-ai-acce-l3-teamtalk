var builder = DistributedApplication.CreateBuilder(args);

var jwtKey = builder.AddParameter("jwt-key", secret: true);
var internalApiKey = builder.AddParameter("internal-api-key", secret: true);

var sqlDb = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("teamtalkdb");

var hub = builder.AddProject<Projects.TeamTalk_Hub>("hub")
    .WithReference(sqlDb)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Internal__ApiKey", internalApiKey)
    .WithHttpEndpoint(port: 5090, name: "http")
    .WaitFor(sqlDb);

var api = builder.AddProject<Projects.TeamTalk_Api>("api")
    .WithReference(sqlDb)
    .WithReference(hub)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Internal__ApiKey", internalApiKey)
    .WithHttpEndpoint(port: 5080, name: "http")
    .WaitFor(sqlDb);

builder.AddJavaScriptApp("site", "../TeamTalk.Site", "start")
    .WithReference(api)
    .WithReference(hub)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
