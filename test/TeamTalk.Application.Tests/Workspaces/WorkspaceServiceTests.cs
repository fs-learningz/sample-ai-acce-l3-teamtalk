using NSubstitute;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Workspaces;
using TeamTalk.Application.Workspaces.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Tests.Workspaces;

public class WorkspaceServiceTests
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly WorkspaceService _sut;

    public WorkspaceServiceTests()
    {
        _workspaceRepository = Substitute.For<IWorkspaceRepository>();
        _workspaceMemberRepository = Substitute.For<IWorkspaceMemberRepository>();
        _sut = new WorkspaceService(_workspaceRepository, _workspaceMemberRepository);
    }

    [Fact]
    public async Task CreateAsync_WithValidName_CreatesWorkspaceAndAdminMember()
    {
        var userId = Guid.NewGuid();
        var request = new CreateWorkspaceRequest { Name = "Engineering" };

        var result = await _sut.CreateAsync(userId, request, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("Engineering", result.Value.Name);
        Assert.Equal(userId, result.Value.CreatedByUserId);
        await _workspaceRepository.Received(1).AddAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>());
        await _workspaceMemberRepository.Received(1).AddAsync(
            Arg.Is<WorkspaceMember>(m => m.UserId == userId && m.IsAdmin()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Workspace?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WorkspaceMember?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), workspace.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsMember_ReturnsCurrentUserRole()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId, Arg.Any<CancellationToken>()).Returns(member);

        var result = await _sut.GetByIdAsync(userId, workspace.Id, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(WorkspaceRole.Admin, result.Value.CurrentUserRole);
    }

    [Fact]
    public async Task DeleteAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Workspace?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WorkspaceMember?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), workspace.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsAdminButNotOwner_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", ownerId, DateTime.UtcNow);
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId, Arg.Any<CancellationToken>()).Returns(member);

        var result = await _sut.DeleteAsync(adminId, workspace.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotOwner, result.FirstError);
        await _workspaceRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsOwner_DeletesWorkspace()
    {
        var ownerId = Guid.NewGuid();
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", ownerId, DateTime.UtcNow);
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, ownerId, Arg.Any<CancellationToken>()).Returns(member);

        var result = await _sut.DeleteAsync(ownerId, workspace.Id, CancellationToken.None);

        Assert.False(result.IsError);
        await _workspaceRepository.Received(1).DeleteAsync(workspace.Id, Arg.Any<CancellationToken>());
    }
}
