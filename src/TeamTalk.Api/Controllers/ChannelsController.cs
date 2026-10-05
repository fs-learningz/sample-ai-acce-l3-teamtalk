using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Channels;
using TeamTalk.Application.Channels.Contracts;
using TeamTalk.Application.Common.Interfaces;

namespace TeamTalk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/channels")]
public class ChannelsController : ControllerBase
{
    private readonly IChannelService _channelService;
    private readonly ICurrentUserService _currentUserService;

    public ChannelsController(IChannelService channelService, ICurrentUserService currentUserService)
    {
        _channelService = channelService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(Guid workspaceId, CreateChannelRequest request)
    {
        var result = await _channelService.CreateAsync(_currentUserService.UserId, workspaceId, request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetVisibleAsync(Guid workspaceId)
    {
        var result = await _channelService.GetVisibleAsync(_currentUserService.UserId, workspaceId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPost("{channelId:guid}/join")]
    public async Task<IActionResult> JoinAsync(Guid workspaceId, Guid channelId)
    {
        var result = await _channelService.JoinAsync(_currentUserService.UserId, workspaceId, channelId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }

    [HttpGet("{channelId:guid}")]
    public async Task<IActionResult> GetDetailsAsync(Guid workspaceId, Guid channelId)
    {
        var result = await _channelService.GetDetailsAsync(_currentUserService.UserId, workspaceId, channelId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPost("{channelId:guid}/read")]
    public async Task<IActionResult> MarkReadAsync(Guid workspaceId, Guid channelId)
    {
        var result = await _channelService.MarkReadAsync(_currentUserService.UserId, workspaceId, channelId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }

    [HttpPut("{channelId:guid}/preferences")]
    public async Task<IActionResult> UpdatePreferencesAsync(
        Guid workspaceId,
        Guid channelId,
        UpdateChannelPreferencesRequest request)
    {
        var result = await _channelService.UpdatePreferencesAsync(
            _currentUserService.UserId,
            workspaceId,
            channelId,
            request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }

    [HttpPost("{channelId:guid}/leave")]
    public async Task<IActionResult> LeaveAsync(Guid workspaceId, Guid channelId)
    {
        var result = await _channelService.LeaveAsync(_currentUserService.UserId, workspaceId, channelId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }

    [HttpDelete("{channelId:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid workspaceId, Guid channelId)
    {
        var result = await _channelService.DeleteAsync(_currentUserService.UserId, workspaceId, channelId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }
}
