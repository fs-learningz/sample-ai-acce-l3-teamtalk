using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Members;
using TeamTalk.Application.Members.Contracts;

namespace TeamTalk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/members")]
public class WorkspaceMembersController : ControllerBase
{
    private readonly IWorkspaceMemberService _memberService;
    private readonly ICurrentUserService _currentUserService;

    public WorkspaceMembersController(IWorkspaceMemberService memberService, ICurrentUserService currentUserService)
    {
        _memberService = memberService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetForWorkspaceAsync(Guid workspaceId)
    {
        var result = await _memberService.GetForWorkspaceAsync(_currentUserService.UserId, workspaceId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPost("leave")]
    public async Task<IActionResult> LeaveAsync(Guid workspaceId)
    {
        var result = await _memberService.LeaveAsync(_currentUserService.UserId, workspaceId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }

    [HttpDelete("{memberUserId:guid}")]
    public async Task<IActionResult> RemoveAsync(Guid workspaceId, Guid memberUserId)
    {
        var result = await _memberService.RemoveAsync(_currentUserService.UserId, workspaceId, memberUserId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }

    [HttpPatch("{memberUserId:guid}/role")]
    public async Task<IActionResult> ChangeRoleAsync(
        Guid workspaceId,
        Guid memberUserId,
        ChangeMemberRoleRequest request)
    {
        var result = await _memberService.ChangeRoleAsync(_currentUserService.UserId, workspaceId, memberUserId, request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }
}
