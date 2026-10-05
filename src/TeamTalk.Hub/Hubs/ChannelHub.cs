using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TeamTalk.Application.Channels;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Hub.Hubs;

[Authorize]
public class ChannelHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly IChannelService _channelService;

    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public ChannelHub(IChannelService channelService, IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _channelService = channelService;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task JoinWorkspaceAsync(Guid workspaceId)
    {
        var isMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, GetUserId());

        if (isMember is false)
        {
            throw new HubException("You are not a member of this workspace.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, WorkspaceGroupName(workspaceId));
    }

    public async Task LeaveWorkspaceAsync(Guid workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, WorkspaceGroupName(workspaceId));
    }

    public async Task JoinChannelAsync(Guid channelId)
    {
        var userId = GetUserId();
        var result = await _channelService.CanUserAccessChannelAsync(channelId, userId);

        if (result.IsError)
        {
            throw new HubException(result.FirstError.Description);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(channelId));
    }

    public async Task LeaveChannelAsync(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(channelId));
    }

    public static string GroupName(Guid channelId)
    {
        return "channel-" + channelId;
    }

    public static string WorkspaceGroupName(Guid workspaceId)
    {
        return "workspace-" + workspaceId;
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);

        if (claim is null)
        {
            throw new HubException("Missing user identity.");
        }

        return Guid.Parse(claim.Value);
    }
}
