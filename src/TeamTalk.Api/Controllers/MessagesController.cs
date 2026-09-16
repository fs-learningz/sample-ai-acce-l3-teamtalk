using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Messages;
using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Api.Controllers;

[Authorize]
[Route("api/workspaces/{workspaceId:guid}/channels/{channelId:guid}/messages")]
public class MessagesController : ApiControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _messageService = messageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistoryAsync(
        Guid workspaceId,
        Guid channelId,
        [FromQuery] DateTime? cursor,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var result = await _messageService.GetHistoryAsync(CurrentUserId, channelId, cursor, pageSize, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> SendAsync(
        Guid workspaceId,
        Guid channelId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageService.SendAsync(CurrentUserId, channelId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("{messageId:guid}")]
    public async Task<IActionResult> EditAsync(
        Guid workspaceId,
        Guid channelId,
        Guid messageId,
        EditMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageService.EditAsync(CurrentUserId, channelId, messageId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid channelId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var result = await _messageService.DeleteAsync(CurrentUserId, workspaceId, channelId, messageId, cancellationToken);
        return result.ToActionResult(this);
    }
}
