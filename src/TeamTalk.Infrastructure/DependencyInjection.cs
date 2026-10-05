using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeamTalk.Application.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Domain.Repositories;
using TeamTalk.Infrastructure.Email;
using TeamTalk.Infrastructure.Identity;
using TeamTalk.Infrastructure.Persistence;
using TeamTalk.Infrastructure.Persistence.Repositories;
using TeamTalk.Infrastructure.Realtime;

namespace TeamTalk.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<TeamTalkDbContext>("teamtalkdb");

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TeamTalkDbContext>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        builder.Services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        builder.Services.AddScoped<IWorkspaceInvitationRepository, WorkspaceInvitationRepository>();
        builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
        builder.Services.AddScoped<IChannelMemberRepository, ChannelMemberRepository>();
        builder.Services.AddScoped<IChannelUserStateRepository, ChannelUserStateRepository>();
        builder.Services.AddScoped<IChannelQueryService, ChannelQueryService>();
        builder.Services.AddScoped<IMessageRepository, MessageRepository>();
        builder.Services.AddScoped<IMessageQueryService, MessageQueryService>();
        builder.Services.AddScoped<IWorkspaceMemberQueryService, WorkspaceMemberQueryService>();
        builder.Services.AddScoped<IWorkspaceQueryService, WorkspaceQueryService>();

        builder.Services.AddScoped<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

        builder.Services.AddHttpClient<IMessageBroadcastService, HubBroadcastService>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri("https+http://hub");

            var internalApiKey = configuration["Internal:ApiKey"];

            if (string.IsNullOrWhiteSpace(internalApiKey) is false)
            {
                client.DefaultRequestHeaders.Add(RealtimeConstants.InternalApiKeyHeaderName, internalApiKey);
            }
        });

        return builder;
    }
}
