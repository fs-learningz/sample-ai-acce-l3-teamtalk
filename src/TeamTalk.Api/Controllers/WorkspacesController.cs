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

    [HttpPut("{workspaceId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var result = await _workspaceService.UpdateAsync(CurrentUserId, workspaceId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("{workspaceId:guid}/icon")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UpdateIconAsync(Guid workspaceId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var result = await _workspaceService.UpdateIconAsync(
            CurrentUserId,
            workspaceId,
            stream.ToArray(),
            file.ContentType,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpDelete("{workspaceId:guid}/icon")]
    public async Task<IActionResult> RemoveIconAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _workspaceService.RemoveIconAsync(CurrentUserId, workspaceId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{workspaceId:guid}/icon")]
    public async Task<IActionResult> GetIconAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _workspaceService.GetIconAsync(CurrentUserId, workspaceId, cancellationToken);
        if (result.IsError)
        {
            return result.ToActionResult(this);
        }

        return File(result.Value.Data, result.Value.ContentType);
    }

    [HttpDelete("{workspaceId:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _workspaceService.DeleteAsync(CurrentUserId, workspaceId, cancellationToken);
        return result.ToActionResult(this);
    }
}
