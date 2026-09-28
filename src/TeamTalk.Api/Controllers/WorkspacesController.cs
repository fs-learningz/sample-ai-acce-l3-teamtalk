using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTalk.Api.Common;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Workspaces;
using TeamTalk.Application.Workspaces.Contracts;

namespace TeamTalk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces")]
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ICurrentUserService _currentUserService;

    public WorkspacesController(IWorkspaceService workspaceService, ICurrentUserService currentUserService)
    {
        _workspaceService = workspaceService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateWorkspaceRequest request)
    {
        var result = await _workspaceService.CreateAsync(_currentUserService.UserId, request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetForCurrentUserAsync()
    {
        var result = await _workspaceService.GetForUserAsync(_currentUserService.UserId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpGet("{workspaceId:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid workspaceId)
    {
        var result = await _workspaceService.GetByIdAsync(_currentUserService.UserId, workspaceId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPut("{workspaceId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid workspaceId, UpdateWorkspaceRequest request)
    {
        var result = await _workspaceService.UpdateAsync(_currentUserService.UserId, workspaceId, request);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpPut("{workspaceId:guid}/icon")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UpdateIconAsync(Guid workspaceId, IFormFile file)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        var result = await _workspaceService.UpdateIconAsync(
            _currentUserService.UserId,
            workspaceId,
            stream.ToArray(),
            file.ContentType);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{workspaceId:guid}/icon")]
    public async Task<IActionResult> RemoveIconAsync(Guid workspaceId)
    {
        var result = await _workspaceService.RemoveIconAsync(_currentUserService.UserId, workspaceId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return Ok(result.Value);
    }

    [HttpGet("{workspaceId:guid}/icon")]
    public async Task<IActionResult> GetIconAsync(Guid workspaceId)
    {
        var result = await _workspaceService.GetIconAsync(_currentUserService.UserId, workspaceId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return File(result.Value.Data, result.Value.ContentType);
    }

    [HttpDelete("{workspaceId:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid workspaceId)
    {
        var result = await _workspaceService.DeleteAsync(_currentUserService.UserId, workspaceId);

        if (result.IsError)
        {
            var error = result.FirstError;

            return Problem(statusCode: error.Type.TypeToCode(), title: error.Code, detail: error.Description);
        }

        return NoContent();
    }
}
