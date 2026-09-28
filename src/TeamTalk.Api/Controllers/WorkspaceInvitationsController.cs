using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Invitations;
using TeamTalk.Application.Invitations.Contracts;

namespace TeamTalk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/invitations")]
public class WorkspaceInvitationsController : ControllerBase
{
    private readonly IWorkspaceInvitationService _invitationService;
    private readonly ICurrentUserService _currentUserService;

    public WorkspaceInvitationsController(IWorkspaceInvitationService invitationService, ICurrentUserService currentUserService)
    {
        _invitationService = invitationService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> InviteAsync(Guid workspaceId, InviteMemberRequest request)
    {
        var result = await _invitationService.InviteAsync(_currentUserService.UserId, workspaceId, request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetForWorkspaceAsync(Guid workspaceId)
    {
        var result = await _invitationService.GetForWorkspaceAsync(_currentUserService.UserId, workspaceId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{invitationId:guid}")]
    public async Task<IActionResult> RevokeAsync(Guid workspaceId, Guid invitationId)
    {
        var result = await _invitationService.RevokeAsync(_currentUserService.UserId, workspaceId, invitationId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }
}
