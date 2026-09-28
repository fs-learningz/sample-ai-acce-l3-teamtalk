using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Messages;
using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/channels/{channelId:guid}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ICurrentUserService _currentUserService;

    public MessagesController(IMessageService messageService, ICurrentUserService currentUserService)
    {
        _messageService = messageService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistoryAsync(
        Guid workspaceId,
        Guid channelId,
        [FromQuery] DateTime? cursor,
        [FromQuery] int pageSize)
    {
        var result = await _messageService.GetHistoryAsync(_currentUserService.UserId, channelId, cursor, pageSize);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SendAsync(
        Guid workspaceId,
        Guid channelId,
        SendMessageRequest request)
    {
        var result = await _messageService.SendAsync(_currentUserService.UserId, channelId, request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPut("{messageId:guid}")]
    public async Task<IActionResult> EditAsync(
        Guid workspaceId,
        Guid channelId,
        Guid messageId,
        EditMessageRequest request)
    {
        var result = await _messageService.EditAsync(_currentUserService.UserId, channelId, messageId, request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid channelId,
        Guid messageId)
    {
        var result = await _messageService.DeleteAsync(_currentUserService.UserId, workspaceId, channelId, messageId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }
}
