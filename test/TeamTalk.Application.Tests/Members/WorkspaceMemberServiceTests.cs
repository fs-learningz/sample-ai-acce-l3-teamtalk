using NSubstitute;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Members;
using TeamTalk.Application.Members.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Tests.Members;

public class WorkspaceMemberServiceTests
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceMemberQueryService _workspaceMemberQueryService;
    private readonly WorkspaceMemberService _sut;

    public WorkspaceMemberServiceTests()
    {
        _workspaceRepository = Substitute.For<IWorkspaceRepository>();
        _workspaceMemberRepository = Substitute.For<IWorkspaceMemberRepository>();
        _workspaceMemberQueryService = Substitute.For<IWorkspaceMemberQueryService>();
        _sut = new WorkspaceMemberService(_workspaceRepository, _workspaceMemberRepository, _workspaceMemberQueryService);
    }

    private static Workspace CreateWorkspace(Guid ownerId)
    {
        return new Workspace(Guid.NewGuid(), "Engineering", ownerId, DateTime.UtcNow);
    }

    private static WorkspaceMember CreateMember(Guid workspaceId, Guid userId, WorkspaceRole role = WorkspaceRole.Member)
    {
        return new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, role, DateTime.UtcNow);
    }

    [Fact]
    public async Task GetForWorkspaceAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Workspace?)null);

        var result = await _sut.GetForWorkspaceAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetForWorkspaceAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WorkspaceMember?)null);

        var result = await _sut.GetForWorkspaceAsync(Guid.NewGuid(), workspace.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task GetForWorkspaceAsync_WhenUserIsMember_ReturnsMemberList()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        var member = CreateMember(workspace.Id, ownerId, WorkspaceRole.Admin);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, ownerId, Arg.Any<CancellationToken>()).Returns(member);

        var expected = new List<WorkspaceMemberResponse>
        {
            new()
            {
                Id = member.Id,
                WorkspaceId = workspace.Id,
                UserId = ownerId,
                DisplayName = "Owner",
                Email = "owner@example.com",
                Role = WorkspaceRole.Admin,
                JoinedAtUtc = member.JoinedAtUtc,
            },
        };
        _workspaceMemberQueryService.GetForWorkspaceAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetForWorkspaceAsync(ownerId, workspace.Id, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(result.Value);
        Assert.Equal("Owner", result.Value[0].DisplayName);
    }

    [Fact]
    public async Task RemoveAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Workspace?)null);

        var result = await _sut.RemoveAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task RemoveAsync_WhenActorIsNotAdmin_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        var actorId = Guid.NewGuid();
        var actor = CreateMember(workspace.Id, actorId, WorkspaceRole.Member);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, actorId, Arg.Any<CancellationToken>()).Returns(actor);

        var result = await _sut.RemoveAsync(actorId, workspace.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task RemoveAsync_WhenAdminTargetsSelf_ReturnsCannotRemoveSelf()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        var adminId = Guid.NewGuid();
        var admin = CreateMember(workspace.Id, adminId, WorkspaceRole.Admin);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId, Arg.Any<CancellationToken>()).Returns(admin);

        var result = await _sut.RemoveAsync(adminId, workspace.Id, adminId, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.CannotRemoveSelf, result.FirstError);
    }

    [Fact]
    public async Task RemoveAsync_WhenTargetIsOwner_ReturnsCannotRemoveOwner()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        var adminId = Guid.NewGuid();
        var admin = CreateMember(workspace.Id, adminId, WorkspaceRole.Admin);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId, Arg.Any<CancellationToken>()).Returns(admin);

        var result = await _sut.RemoveAsync(adminId, workspace.Id, ownerId, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.CannotRemoveOwner, result.FirstError);
    }

    [Fact]
    public async Task RemoveAsync_WhenTargetIsNotAMember_ReturnsMemberNotFound()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        var adminId = Guid.NewGuid();
        var admin = CreateMember(workspace.Id, adminId, WorkspaceRole.Admin);
        var targetId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId, Arg.Any<CancellationToken>()).Returns(admin);
        _workspaceMemberRepository.GetAsync(workspace.Id, targetId, Arg.Any<CancellationToken>()).Returns((WorkspaceMember?)null);

        var result = await _sut.RemoveAsync(adminId, workspace.Id, targetId, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.MemberNotFound, result.FirstError);
    }

    [Fact]
    public async Task RemoveAsync_WhenValid_RemovesMember()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        var adminId = Guid.NewGuid();
        var admin = CreateMember(workspace.Id, adminId, WorkspaceRole.Admin);
        var targetId = Guid.NewGuid();
        var target = CreateMember(workspace.Id, targetId);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId, Arg.Any<CancellationToken>()).Returns(admin);
        _workspaceMemberRepository.GetAsync(workspace.Id, targetId, Arg.Any<CancellationToken>()).Returns(target);

        var result = await _sut.RemoveAsync(adminId, workspace.Id, targetId, CancellationToken.None);

        Assert.False(result.IsError);
        await _workspaceMemberRepository.Received(1).RemoveAsync(workspace.Id, targetId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WorkspaceMember?)null);

        var result = await _sut.LeaveAsync(Guid.NewGuid(), workspace.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task LeaveAsync_WhenUserIsOwner_ReturnsOwnerCannotLeave()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        var member = CreateMember(workspace.Id, ownerId, WorkspaceRole.Admin);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, ownerId, Arg.Any<CancellationToken>()).Returns(member);

        var result = await _sut.LeaveAsync(ownerId, workspace.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.OwnerCannotLeave, result.FirstError);
        await _workspaceMemberRepository.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveAsync_WhenUserIsRegularMember_RemovesMembership()
    {
        var ownerId = Guid.NewGuid();
        var workspace = CreateWorkspace(ownerId);
        var userId = Guid.NewGuid();
        var member = CreateMember(workspace.Id, userId);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId, Arg.Any<CancellationToken>()).Returns(member);

        var result = await _sut.LeaveAsync(userId, workspace.Id, CancellationToken.None);

        Assert.False(result.IsError);
        await _workspaceMemberRepository.Received(1).RemoveAsync(workspace.Id, userId, Arg.Any<CancellationToken>());
    }
}
