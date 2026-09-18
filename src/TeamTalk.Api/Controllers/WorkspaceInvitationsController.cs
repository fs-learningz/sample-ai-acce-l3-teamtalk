using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Invitations;
using TeamTalk.Application.Invitations.Contracts;

namespace TeamTalk.Api.Controllers;

[Authorize]
[Route("api/workspaces/{workspaceId:guid}/invitations")]
public class WorkspaceInvitationsController : ApiControllerBase
{
    private readonly IWorkspaceInvitationService _invitationService;

    public WorkspaceInvitationsController(IWorkspaceInvitationService invitationService, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _invitationService = invitationService;
    }

    [HttpPost]
    public async Task<IActionResult> InviteAsync(Guid workspaceId, InviteMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await _invitationService.InviteAsync(CurrentUserId, workspaceId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<IActionResult> GetForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _invitationService.GetForWorkspaceAsync(CurrentUserId, workspaceId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{invitationId:guid}")]
    public async Task<IActionResult> RevokeAsync(Guid workspaceId, Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _invitationService.RevokeAsync(CurrentUserId, workspaceId, invitationId, cancellationToken);
        return result.ToActionResult(this);
    }
}
