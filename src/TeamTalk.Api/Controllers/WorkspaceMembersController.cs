using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Members;

namespace TeamTalk.Api.Controllers;

[Authorize]
[Route("api/workspaces/{workspaceId:guid}/members")]
public class WorkspaceMembersController : ApiControllerBase
{
    private readonly IWorkspaceMemberService _memberService;

    public WorkspaceMembersController(IWorkspaceMemberService memberService, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _memberService = memberService;
    }

    [HttpGet]
    public async Task<IActionResult> GetForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _memberService.GetForWorkspaceAsync(CurrentUserId, workspaceId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("leave")]
    public async Task<IActionResult> LeaveAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _memberService.LeaveAsync(CurrentUserId, workspaceId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{memberUserId:guid}")]
    public async Task<IActionResult> RemoveAsync(Guid workspaceId, Guid memberUserId, CancellationToken cancellationToken)
    {
        var result = await _memberService.RemoveAsync(CurrentUserId, workspaceId, memberUserId, cancellationToken);
        return result.ToActionResult(this);
    }
}
