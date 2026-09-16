using NSubstitute;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Workspaces;
using TeamTalk.Application.Workspaces.Contracts;
using TeamTalk.Domain.Entities;
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
        _workspaceMemberRepository.IsMemberAsync(workspace.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), workspace.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }
}
