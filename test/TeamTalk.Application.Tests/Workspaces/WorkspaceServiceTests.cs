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
        // arrange
        var userId = Guid.NewGuid();
        var request = new CreateWorkspaceRequest { Name = "Engineering" };

        // act
        var result = await _sut.CreateAsync(userId, request);

        // assert
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
        // arrange
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        // act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsMember_ReturnsCurrentUserRoleAndSummary()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId).Returns(member);
        _workspaceQueryService.GetSummaryAsync(workspace.Id)
            .Returns(new WorkspaceSummary("Creator", "creator@example.com", 3));

        // act
        var result = await _sut.GetByIdAsync(userId, workspace.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(WorkspaceRole.Admin, result.Value.CurrentUserRole);
        Assert.Equal("Creator", result.Value.CreatedByDisplayName);
        Assert.Equal(3, result.Value.MemberCount);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var memberId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, memberId).Returns(member);

        // act
        var result = await _sut.UpdateAsync(
            memberId,
            workspace.Id,
            new UpdateWorkspaceRequest { Name = "New name" });

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameExceedsMaxLength_ReturnsValidationError()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        // act
        var result = await _sut.UpdateAsync(
            adminId,
            workspace.Id,
            new UpdateWorkspaceRequest { Name = new string('a', Workspace.MaxNameLength + 1) });

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Workspace.NameTooLong", result.FirstError.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesNameAndDescription()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        // act
        var result = await _sut.UpdateAsync(
            adminId,
            workspace.Id,
            new UpdateWorkspaceRequest { Name = "Renamed", Description = "New description" });

        // assert
        Assert.False(result.IsError);
        Assert.Equal("Renamed", result.Value.Name);
        Assert.Equal("New description", result.Value.Description);
        await _workspaceRepository.Received(1).UpdateAsync(workspace);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenIconTooLarge_ReturnsValidationError()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        var oversizedIcon = new byte[2 * 1024 * 1024 + 1];

        // act
        var result = await _sut.UpdateIconAsync(adminId, workspace.Id, oversizedIcon, "image/png");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.IconTooLarge, result.FirstError);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenContentTypeUnsupported_ReturnsValidationError()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        // act
        var result = await _sut.UpdateIconAsync(adminId, workspace.Id, [1, 2, 3], "image/gif");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.UnsupportedIconFormat, result.FirstError);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenValid_SetsIcon()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var adminId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        // act
        var result = await _sut.UpdateIconAsync(adminId, workspace.Id, [1, 2, 3], "image/png");

        // assert
        Assert.False(result.IsError);
        Assert.True(result.Value.HasIcon);
        await _workspaceRepository.Received(1).UpdateAsync(workspace);
    }

    [Fact]
    public async Task GetIconAsync_WhenWorkspaceHasNoIcon_ReturnsNotFound()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.IsMemberAsync(workspace.Id, userId).Returns(true);

        // act
        var result = await _sut.GetIconAsync(userId, workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.IconNotFound, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        // act
        var result = await _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.DeleteAsync(Guid.NewGuid(), workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsMemberNotAdmin_ReturnsForbidden()
    {
        // arrange
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", creatorId, DateTime.UtcNow);
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, memberId).Returns(member);

        // act
        var result = await _sut.DeleteAsync(memberId, workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
        await _workspaceRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsAdminButNotCreator_DeletesWorkspace()
    {
        // arrange
        var creatorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", creatorId, DateTime.UtcNow);
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, adminId).Returns(admin);

        // act
        var result = await _sut.DeleteAsync(adminId, workspace.Id);

        // assert
        Assert.False(result.IsError);
        await _workspaceRepository.Received(1).DeleteAsync(workspace.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithBlankName_ReturnsValidationError(string name)
    {
        // arrange
        var request = new CreateWorkspaceRequest { Name = name };

        // act
        var result = await _sut.CreateAsync(Guid.NewGuid(), request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Workspace.NameRequired", result.FirstError.Code);
        await _workspaceRepository.DidNotReceive().AddAsync(Arg.Any<Workspace>());
    }

    [Fact]
    public async Task CreateAsync_WithNameExceedingMaxLength_ReturnsValidationError()
    {
        // arrange
        var request = new CreateWorkspaceRequest { Name = new string('a', Workspace.MaxNameLength + 1) };

        // act
        var result = await _sut.CreateAsync(Guid.NewGuid(), request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Workspace.NameTooLong", result.FirstError.Code);
        await _workspaceRepository.DidNotReceive().AddAsync(Arg.Any<Workspace>());
    }

    [Fact]
    public async Task CreateAsync_WithNameOfExactlyMaxLength_Succeeds()
    {
        // arrange
        var request = new CreateWorkspaceRequest { Name = new string('a', Workspace.MaxNameLength) };

        // act
        var result = await _sut.CreateAsync(Guid.NewGuid(), request);

        // assert
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsMappedWorkspaces()
    {
        // arrange
        var userId = Guid.NewGuid();
        var first = new Workspace(Guid.NewGuid(), "First", userId, DateTime.UtcNow);
        var second = new Workspace(Guid.NewGuid(), "Second", userId, DateTime.UtcNow);
        _workspaceRepository.GetForUserAsync(userId).Returns(new List<Workspace> { first, second });

        // act
        var result = await _sut.GetForUserAsync(userId);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(["First", "Second"], result.Value.Select(w => w.Name).ToArray());
    }

    [Fact]
    public async Task GetForUserAsync_WhenUserHasNoWorkspaces_ReturnsEmptyList()
    {
        // arrange
        _workspaceRepository.GetForUserAsync(Arg.Any<Guid>()).Returns(new List<Workspace>());

        // act
        var result = await _sut.GetForUserAsync(Guid.NewGuid());

        // assert
        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task UpdateAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        // act
        var request = new UpdateWorkspaceRequest { Name = "Renamed" };
        var result = await _sut.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var request = new UpdateWorkspaceRequest { Name = "Renamed" };
        var result = await _sut.UpdateAsync(Guid.NewGuid(), workspace.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_WhenNameIsBlank_ReturnsValidationError(string name)
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow));

        // act
        var request = new UpdateWorkspaceRequest { Name = name };
        var result = await _sut.UpdateAsync(userId, workspace.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Workspace.NameRequired", result.FirstError.Code);
        await _workspaceRepository.DidNotReceive().UpdateAsync(Arg.Any<Workspace>());
    }

    [Fact]
    public async Task UpdateAsync_WhenDescriptionExceedsMaxLength_ReturnsValidationError()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow));

        // act
        var request = new UpdateWorkspaceRequest
        {
            Name = "Engineering",
            Description = new string('d', Workspace.MaxDescriptionLength + 1),
        };
        var result = await _sut.UpdateAsync(userId, workspace.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Workspace.DescriptionTooLong", result.FirstError.Code);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        // act
        var result = await _sut.UpdateIconAsync(Guid.NewGuid(), Guid.NewGuid(), [1], "image/png");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.UpdateIconAsync(Guid.NewGuid(), workspace.Id, [1], "image/png");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Member, DateTime.UtcNow));

        // act
        var result = await _sut.UpdateIconAsync(userId, workspace.Id, [1], "image/png");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task UpdateIconAsync_WhenIconIsExactlyMaxSize_SetsIcon()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow));

        // act
        var result = await _sut.UpdateIconAsync(userId, workspace.Id, new byte[2 * 1024 * 1024], "image/png");

        // assert
        Assert.False(result.IsError);
        Assert.True(result.Value.HasIcon);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/svg+xml")]
    [InlineData("IMAGE/PNG")]
    public async Task UpdateIconAsync_WithSupportedContentType_SetsIcon(string contentType)
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow));

        // act
        var result = await _sut.UpdateIconAsync(userId, workspace.Id, [1, 2, 3], contentType);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(contentType, workspace.IconContentType);
    }

    [Fact]
    public async Task RemoveIconAsync_WhenAdmin_RemovesIconAndPersists()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([1, 2, 3], "image/png", DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow));

        // act
        var result = await _sut.RemoveIconAsync(userId, workspace.Id);

        // assert
        Assert.False(result.IsError);
        Assert.False(result.Value.HasIcon);
        await _workspaceRepository.Received(1).UpdateAsync(workspace);
    }

    [Fact]
    public async Task RemoveIconAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        // act
        var result = await _sut.RemoveIconAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task RemoveIconAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.RemoveIconAsync(Guid.NewGuid(), workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task RemoveIconAsync_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Member, DateTime.UtcNow));

        // act
        var result = await _sut.RemoveIconAsync(userId, workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
        await _workspaceRepository.DidNotReceive().UpdateAsync(Arg.Any<Workspace>());
    }

    [Fact]
    public async Task GetIconAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        // act
        var result = await _sut.GetIconAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetIconAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([1], "image/png", DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.IsMemberAsync(workspace.Id, Arg.Any<Guid>()).Returns(false);

        // act
        var result = await _sut.GetIconAsync(Guid.NewGuid(), workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task GetIconAsync_WhenMemberAndWorkspaceHasIcon_ReturnsIconData()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([9, 8, 7], "image/jpeg", DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.IsMemberAsync(workspace.Id, userId).Returns(true);

        // act
        var result = await _sut.GetIconAsync(userId, workspace.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(new byte[] { 9, 8, 7 }, result.Value.Data);
        Assert.Equal("image/jpeg", result.Value.ContentType);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsNotMember_ReturnsForbiddenWithoutDeleting()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.DeleteAsync(Guid.NewGuid(), workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
        await _workspaceRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }
}
