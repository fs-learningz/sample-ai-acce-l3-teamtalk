using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Invitations;

namespace TeamTalk.Api.Controllers;

[Authorize]
[Route("api/invitations")]
public class InvitationsController : ApiControllerBase
{
    private readonly IWorkspaceInvitationService _invitationService;

    public InvitationsController(IWorkspaceInvitationService invitationService, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _invitationService = invitationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var result = await _invitationService.GetPendingForCurrentUserAsync(CurrentUserId, CurrentUserEmail, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{invitationId:guid}/accept")]
    public async Task<IActionResult> AcceptAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _invitationService.AcceptAsync(CurrentUserId, CurrentUserEmail, invitationId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{invitationId:guid}/decline")]
    public async Task<IActionResult> DeclineAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _invitationService.DeclineAsync(CurrentUserId, CurrentUserEmail, invitationId, cancellationToken);
        return result.ToActionResult(this);
    }
}
