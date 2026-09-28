using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Invitations;

namespace TeamTalk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IWorkspaceInvitationService _invitationService;
    private readonly ICurrentUserService _currentUserService;

    public InvitationsController(IWorkspaceInvitationService invitationService, ICurrentUserService currentUserService)
    {
        _invitationService = invitationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingForCurrentUserAsync()
    {
        var result = await _invitationService.GetPendingForCurrentUserAsync(_currentUserService.UserId, _currentUserService.Email);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPost("{invitationId:guid}/accept")]
    public async Task<IActionResult> AcceptAsync(Guid invitationId)
    {
        var result = await _invitationService.AcceptAsync(_currentUserService.UserId, _currentUserService.Email, invitationId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }

    [HttpPost("{invitationId:guid}/decline")]
    public async Task<IActionResult> DeclineAsync(Guid invitationId)
    {
        var result = await _invitationService.DeclineAsync(_currentUserService.UserId, _currentUserService.Email, invitationId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }

    [HttpGet("{invitationId:guid}/workspace-icon")]
    public async Task<IActionResult> GetWorkspaceIconAsync(Guid invitationId)
    {
        var result = await _invitationService.GetWorkspaceIconAsync(_currentUserService.Email, invitationId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return File(result.Value.Data, result.Value.ContentType);
    }
}
