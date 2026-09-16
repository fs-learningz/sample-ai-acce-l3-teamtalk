using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using TeamTalk.Application;
using TeamTalk.Application.Common;
using TeamTalk.Application.Common.Models;
using TeamTalk.Hub.Hubs;
using TeamTalk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.AddInfrastructure();

builder.Services.AddSignalR();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key configuration is missing.");
}

var internalApiKey = builder.Configuration["Internal:ApiKey"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/channel"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

const string DevCorsPolicyName = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicyName, policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicyName);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChannelHub>("/hubs/channel");

app.MapPost("/internal/broadcast/channels/{channelId:guid}", async (
    Guid channelId,
    BroadcastMessageRequest body,
    HttpRequest request,
    IHubContext<ChannelHub> hubContext) =>
{
    var providedApiKey = request.Headers[RealtimeConstants.InternalApiKeyHeaderName].ToString();
    if (string.IsNullOrWhiteSpace(internalApiKey) || providedApiKey != internalApiKey)
    {
        return Results.Unauthorized();
    }

    await hubContext.Clients.Group(ChannelHub.GroupName(channelId)).SendAsync(body.EventName, body.Message);
    return Results.Ok();
});

app.Run();
