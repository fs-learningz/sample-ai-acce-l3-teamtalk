using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Channels;
using TeamTalk.Application.Channels.Contracts;
using TeamTalk.Application.Common.Interfaces;

namespace TeamTalk.Api.Controllers;

[Authorize]
[Route("api/workspaces/{workspaceId:guid}/channels")]
public class ChannelsController : ApiControllerBase
{
    private readonly IChannelService _channelService;

    public ChannelsController(IChannelService channelService, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _channelService = channelService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(Guid workspaceId, CreateChannelRequest request, CancellationToken cancellationToken)
    {
        var result = await _channelService.CreateAsync(CurrentUserId, workspaceId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<IActionResult> GetVisibleAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _channelService.GetVisibleAsync(CurrentUserId, workspaceId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{channelId:guid}/join")]
    public async Task<IActionResult> JoinAsync(Guid workspaceId, Guid channelId, CancellationToken cancellationToken)
    {
        var result = await _channelService.JoinAsync(CurrentUserId, workspaceId, channelId, cancellationToken);
        return result.ToActionResult(this);
    }
}
