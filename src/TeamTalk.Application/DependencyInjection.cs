using Microsoft.Extensions.DependencyInjection;
using TeamTalk.Application.Auth;
using TeamTalk.Application.Channels;
using TeamTalk.Application.Invitations;
using TeamTalk.Application.Messages;
using TeamTalk.Application.Workspaces;

namespace TeamTalk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IWorkspaceInvitationService, WorkspaceInvitationService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IMessageService, MessageService>();

        return services;
    }
}
