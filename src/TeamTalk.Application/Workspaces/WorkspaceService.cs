using ErrorOr;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Workspaces.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Workspaces;

public class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public WorkspaceService(IWorkspaceRepository workspaceRepository, IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task<ErrorOr<WorkspaceResponse>> CreateAsync(Guid userId, CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Error.Validation("Workspace.NameRequired", "Workspace name is required.");
        }

        var nowUtc = DateTime.UtcNow;
        var workspace = new Workspace(Guid.CreateVersion7(), request.Name, userId, nowUtc);
        await _workspaceRepository.AddAsync(workspace, cancellationToken);

        var member = new WorkspaceMember(Guid.CreateVersion7(), workspace.Id, userId, WorkspaceRole.Admin, nowUtc);
        await _workspaceMemberRepository.AddAsync(member, cancellationToken);

        return ToResponse(workspace, member.Role);
    }

    public async Task<ErrorOr<IReadOnlyList<WorkspaceResponse>>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var workspaces = await _workspaceRepository.GetForUserAsync(userId, cancellationToken);
        return workspaces.Select(w => ToResponse(w)).ToList();
    }

    public async Task<ErrorOr<WorkspaceResponse>> GetByIdAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId, cancellationToken);
        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        return ToResponse(workspace, member.Role);
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId, cancellationToken);
        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (workspace.CreatedByUserId != userId)
        {
            return WorkspaceErrors.NotOwner;
        }

        await _workspaceRepository.DeleteAsync(workspaceId, cancellationToken);

        return Result.Success;
    }

    private static WorkspaceResponse ToResponse(Workspace workspace, WorkspaceRole? currentUserRole = null)
    {
        return new WorkspaceResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            CreatedByUserId = workspace.CreatedByUserId,
            CreatedAtUtc = workspace.CreatedAtUtc,
            CurrentUserRole = currentUserRole,
        };
    }
}
