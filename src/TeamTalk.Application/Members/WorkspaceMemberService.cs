using ErrorOr;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Members.Contracts;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Members;

public class WorkspaceMemberService : IWorkspaceMemberService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceMemberQueryService _workspaceMemberQueryService;

    public WorkspaceMemberService(
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceMemberQueryService workspaceMemberQueryService)
    {
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceMemberQueryService = workspaceMemberQueryService;
    }

    public async Task<ErrorOr<IReadOnlyList<WorkspaceMemberResponse>>> GetForWorkspaceAsync(
        Guid userId,
        Guid workspaceId)
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

        var members = await _workspaceMemberQueryService.GetForWorkspaceAsync(workspaceId);

        return members.ToList();
    }

    public async Task<ErrorOr<Success>> RemoveAsync(
        Guid adminUserId,
        Guid workspaceId,
        Guid memberUserId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var admin = await _workspaceMemberRepository.GetAsync(workspaceId, adminUserId);

        if (admin is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (admin.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        if (memberUserId == adminUserId)
        {
            return WorkspaceErrors.CannotRemoveSelf;
        }

        var target = await _workspaceMemberRepository.GetAsync(workspaceId, memberUserId);

        if (target is null)
        {
            return WorkspaceErrors.MemberNotFound;
        }

        await _workspaceMemberRepository.RemoveAsync(workspaceId, memberUserId);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> LeaveAsync(Guid userId, Guid workspaceId)
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

        var left = await _workspaceMemberRepository.TryLeaveAsync(workspaceId, userId);

        if (left is false)
        {
            return WorkspaceErrors.LastAdmin;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ChangeRoleAsync(
        Guid adminUserId,
        Guid workspaceId,
        Guid memberUserId,
        ChangeMemberRoleRequest request)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var admin = await _workspaceMemberRepository.GetAsync(workspaceId, adminUserId);

        if (admin is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (admin.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        var target = await _workspaceMemberRepository.GetAsync(workspaceId, memberUserId);

        if (target is null)
        {
            return WorkspaceErrors.MemberNotFound;
        }

        var changed = await _workspaceMemberRepository.TrySetRoleAsync(workspaceId, memberUserId, request.Role);

        if (changed is false)
        {
            return WorkspaceErrors.LastAdmin;
        }

        return Result.Success;
    }
}
