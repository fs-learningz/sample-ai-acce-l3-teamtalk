using NSubstitute;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Common.Models;
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
    private readonly IWorkspaceQueryService _workspaceQueryService;
    private readonly WorkspaceService _sut;

    public WorkspaceServiceTests()
    {
        _workspaceRepository = Substitute.For<IWorkspaceRepository>();
        _workspaceMemberRepository = Substitute.For<IWorkspaceMemberRepository>();
        _workspaceQueryService = Substitute.For<IWorkspaceQueryService>();
        _sut = new WorkspaceService(_workspaceRepository, _workspaceMemberRepository, _workspaceQueryService);
    }

    [Fact]
    public async Task CreateAsync_WithValidName_CreatesWorkspaceAndAdminMember()
    {
        var userId = Guid.NewGuid();
        var request = new CreateWorkspaceRequest { Name = "Engineering" };

        var result = await _sut.CreateAsync(userId, request);

        Assert.False(result.IsError);
        Assert.Equal("Engineering", result.Value.Name);
        Assert.Equal(userId, result.Value.CreatedByUserId);
        await _workspaceRepository.Received(1).AddAsync(Arg.Any<Workspace>());
        await _workspaceMemberRepository.Received(1).AddAsync(
            Arg.Is<WorkspaceMember>(m => m.UserId == userId && m.IsAdmin()));
    }

    [Fact]
    public async Task GetByIdAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), workspace.Id);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsMember_ReturnsCurrentUserRoleAndSummary()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId).Returns(member);
        _workspaceQueryService.GetSummaryAsync(workspace.Id)
            .Returns(new WorkspaceSummary("Creator", "creator@example.com", 3));

        var result = await _sut.GetByIdAsync(userId, workspace.Id);

        Assert.False(result.IsError);
        Assert.Equal(WorkspaceRole.Admin, result.Value.CurrentUserRole);
        Assert.Equal("Creator", result.Value.CreatedByDisplayName);
        Assert.Equal(3, result.Value.MemberCount);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var memberId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, memberId).Returns(member);

        var result = await _sut.UpdateAsync(
            memberId,
            workspace.Id,
            new UpdateWorkspaceRequest { Name = "New name" });

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameExceedsMaxLength_ReturnsValidationError()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        var result = await _sut.UpdateAsync(
            adminId,
            workspace.Id,
            new UpdateWorkspaceRequest { Name = new string('a', Workspace.MaxNameLength + 1) });

        Assert.True(result.IsError);
        Assert.Equal("Workspace.NameTooLong", result.FirstError.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesNameAndDescription()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        var result = await _sut.UpdateAsync(
            adminId,
            workspace.Id,
            new UpdateWorkspaceRequest { Name = "Renamed", Description = "New description" });

        Assert.False(result.IsError);
        Assert.Equal("Renamed", result.Value.Name);
        Assert.Equal("New description", result.Value.Description);
        await _workspaceRepository.Received(1).UpdateAsync(workspace);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenIconTooLarge_ReturnsValidationError()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        var oversizedIcon = new byte[2 * 1024 * 1024 + 1];

        var result = await _sut.UpdateIconAsync(adminId, workspace.Id, oversizedIcon, "image/png");

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.IconTooLarge, result.FirstError);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenContentTypeUnsupported_ReturnsValidationError()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        var result = await _sut.UpdateIconAsync(adminId, workspace.Id, [1, 2, 3], "image/gif");

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.UnsupportedIconFormat, result.FirstError);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenValid_SetsIcon()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        var result = await _sut.UpdateIconAsync(adminId, workspace.Id, [1, 2, 3], "image/png");

        Assert.False(result.IsError);
        Assert.True(result.Value.HasIcon);
        await _workspaceRepository.Received(1).UpdateAsync(workspace);
    }

    [Fact]
    public async Task GetIconAsync_WhenWorkspaceHasNoIcon_ReturnsNotFound()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.IsMemberAsync(workspace.Id, userId).Returns(true);

        var result = await _sut.GetIconAsync(userId, workspace.Id);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.IconNotFound, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), workspace.Id);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsMemberNotAdmin_ReturnsForbidden()
    {
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", creatorId, DateTime.UtcNow);
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, memberId).Returns(member);

        var result = await _sut.DeleteAsync(memberId, workspace.Id);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
        await _workspaceRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsAdminButNotCreator_DeletesWorkspace()
    {
        var creatorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", creatorId, DateTime.UtcNow);
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        var result = await _sut.DeleteAsync(adminId, workspace.Id);

        Assert.False(result.IsError);
        await _workspaceRepository.Received(1).DeleteAsync(workspace.Id);
    }
}
