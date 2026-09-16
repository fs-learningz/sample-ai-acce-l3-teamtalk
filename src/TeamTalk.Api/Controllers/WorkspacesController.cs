using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Workspaces;
using TeamTalk.Application.Workspaces.Contracts;

namespace TeamTalk.Api.Controllers;

[Authorize]
[Route("api/workspaces")]
public class WorkspacesController : ApiControllerBase
{
    private readonly IWorkspaceService _workspaceService;

    public WorkspacesController(IWorkspaceService workspaceService, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _workspaceService = workspaceService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var result = await _workspaceService.CreateAsync(CurrentUserId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<IActionResult> GetForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var result = await _workspaceService.GetForUserAsync(CurrentUserId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{workspaceId:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _workspaceService.GetByIdAsync(CurrentUserId, workspaceId, cancellationToken);
        return result.ToActionResult(this);
    }
}
