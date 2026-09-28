using ErrorOr;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Common.Models;
using TeamTalk.Application.Workspaces.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Workspaces;

public class WorkspaceService : IWorkspaceService
{
    private static readonly HashSet<string> AllowedIconContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/svg+xml",
    };

    private const long MaxIconSizeBytes = 2 * 1024 * 1024;

    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceQueryService _workspaceQueryService;

    public WorkspaceService(
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceQueryService workspaceQueryService)
    {
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceQueryService = workspaceQueryService;
    }

    public async Task<ErrorOr<WorkspaceResponse>> CreateAsync(Guid userId, CreateWorkspaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Error.Validation("Workspace.NameRequired", "Workspace name is required.");
        }

        if (request.Name.Trim().Length > Workspace.MaxNameLength)
        {
            return Error.Validation("Workspace.NameTooLong", $"Workspace name cannot exceed {Workspace.MaxNameLength} characters.");
        }

        var nowUtc = DateTime.UtcNow;
        var workspace = new Workspace(Guid.CreateVersion7(), request.Name, userId, nowUtc);
        await _workspaceRepository.AddAsync(workspace);

        var member = new WorkspaceMember(Guid.CreateVersion7(), workspace.Id, userId, WorkspaceRole.Admin, nowUtc);
        await _workspaceMemberRepository.AddAsync(member);

        return ToResponse(workspace, member.Role);
    }

    public async Task<ErrorOr<IReadOnlyList<WorkspaceResponse>>> GetForUserAsync(Guid userId)
    {
        var workspaces = await _workspaceRepository.GetForUserAsync(userId);

        return workspaces.Select(w => ToResponse(w)).ToList();
    }

    public async Task<ErrorOr<WorkspaceResponse>> GetByIdAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId);

        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        var summary = await _workspaceQueryService.GetSummaryAsync(workspaceId);

        return ToResponse(workspace, member.Role, summary);
    }

    public async Task<ErrorOr<WorkspaceResponse>> UpdateAsync(
        Guid userId,
        Guid workspaceId,
        UpdateWorkspaceRequest request)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId);

        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (member.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Error.Validation("Workspace.NameRequired", "Workspace name is required.");
        }

        if (request.Name.Trim().Length > Workspace.MaxNameLength)
        {
            return Error.Validation("Workspace.NameTooLong", $"Workspace name cannot exceed {Workspace.MaxNameLength} characters.");
        }

        if (request.Description?.Trim().Length > Workspace.MaxDescriptionLength)
        {
            return Error.Validation("Workspace.DescriptionTooLong", $"Workspace description cannot exceed {Workspace.MaxDescriptionLength} characters.");
        }

        workspace.UpdateDetails(request.Name, request.Description, DateTime.UtcNow);
        await _workspaceRepository.UpdateAsync(workspace);

        var summary = await _workspaceQueryService.GetSummaryAsync(workspaceId);

        return ToResponse(workspace, member.Role, summary);
    }

    public async Task<ErrorOr<WorkspaceResponse>> UpdateIconAsync(
        Guid userId,
        Guid workspaceId,
        byte[] iconData,
        string contentType)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId);

        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (member.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        if (iconData.Length > MaxIconSizeBytes)
        {
            return WorkspaceErrors.IconTooLarge;
        }

        if (AllowedIconContentTypes.Contains(contentType) is false)
        {
            return WorkspaceErrors.UnsupportedIconFormat;
        }

        workspace.SetIcon(iconData, contentType, DateTime.UtcNow);
        await _workspaceRepository.UpdateAsync(workspace);

        var summary = await _workspaceQueryService.GetSummaryAsync(workspaceId);

        return ToResponse(workspace, member.Role, summary);
    }

    public async Task<ErrorOr<WorkspaceResponse>> RemoveIconAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId);

        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (member.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        workspace.RemoveIcon(DateTime.UtcNow);
        await _workspaceRepository.UpdateAsync(workspace);

        var summary = await _workspaceQueryService.GetSummaryAsync(workspaceId);

        return ToResponse(workspace, member.Role, summary);
    }

    public async Task<ErrorOr<WorkspaceIconResponse>> GetIconAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var isMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, userId);

        if (isMember is false)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (workspace.IconData is null || workspace.IconContentType is null)
        {
            return WorkspaceErrors.IconNotFound;
        }

        return new WorkspaceIconResponse { Data = workspace.IconData, ContentType = workspace.IconContentType };
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId);

        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (member.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        await _workspaceRepository.DeleteAsync(workspaceId);

        return Result.Success;
    }

    private static WorkspaceResponse ToResponse(
        Workspace workspace,
        WorkspaceRole? currentUserRole = null,
        WorkspaceSummary? summary = null)
    {
        return new WorkspaceResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            HasIcon = workspace.HasIcon,
            CreatedByUserId = workspace.CreatedByUserId,
            CreatedByDisplayName = summary?.CreatedByDisplayName,
            CreatedAtUtc = workspace.CreatedAtUtc,
            MemberCount = summary?.MemberCount,
            CurrentUserRole = currentUserRole,
        };
    }
}
