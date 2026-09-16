using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TeamTalk.Application.Channels;

namespace TeamTalk.Hub.Hubs;

[Authorize]
public class ChannelHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly IChannelService _channelService;

    public ChannelHub(IChannelService channelService)
    {
        _channelService = channelService;
    }

    public async Task JoinChannelAsync(Guid channelId)
    {
        var userId = GetUserId();
        var result = await _channelService.CanUserAccessChannelAsync(channelId, userId, Context.ConnectionAborted);
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
