using Microsoft.Extensions.Logging;
using NSubstitute;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Invitations;
using TeamTalk.Application.Invitations.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Tests.Invitations;

public class WorkspaceInvitationServiceTests
{
    private readonly IWorkspaceInvitationRepository _invitationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IIdentityService _identityService;
    private readonly IEmailSender _emailSender;
    private readonly WorkspaceInvitationService _sut;

    public WorkspaceInvitationServiceTests()
    {
        _invitationRepository = Substitute.For<IWorkspaceInvitationRepository>();
        _workspaceRepository = Substitute.For<IWorkspaceRepository>();
        _workspaceMemberRepository = Substitute.For<IWorkspaceMemberRepository>();
        _identityService = Substitute.For<IIdentityService>();
        _emailSender = Substitute.For<IEmailSender>();
        _sut = new WorkspaceInvitationService(
            _invitationRepository,
            _workspaceRepository,
            _workspaceMemberRepository,
            _identityService,
            _emailSender,
            Substitute.For<ILogger<WorkspaceInvitationService>>());
    }

    [Fact]
    public async Task InviteAsync_WhenInviterIsNotAdmin_ReturnsForbidden()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, inviterId, WorkspaceRole.Member, DateTime.UtcNow);

        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, inviterId).Returns(member);

        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "new@example.com" });

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task InviteAsync_WhenTargetAlreadyAMember_ReturnsConflict()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, inviterId, WorkspaceRole.Admin, DateTime.UtcNow);
        var existingUserId = Guid.NewGuid();

        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, inviterId).Returns(admin);
        _identityService.FindUserIdByEmailAsync("existing@example.com").Returns(existingUserId);
        _workspaceMemberRepository.IsMemberAsync(workspace.Id, existingUserId).Returns(true);

        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "existing@example.com" });

        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.AlreadyAMember, result.FirstError);
    }

    [Fact]
    public async Task InviteAsync_WhenPendingInvitationAlreadyExists_ReturnsConflict()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, inviterId, WorkspaceRole.Admin, DateTime.UtcNow);
        var existingInvitation = new WorkspaceInvitation(Guid.NewGuid(), workspace.Id, "new@example.com", WorkspaceRole.Member, inviterId, DateTime.UtcNow);

        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, inviterId).Returns(admin);
        _identityService.FindUserIdByEmailAsync("new@example.com").Returns((Guid?)null);
        _invitationRepository.GetPendingByWorkspaceAndEmailAsync(workspace.Id, "new@example.com").Returns(existingInvitation);

        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "new@example.com" });

        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.AlreadyPending, result.FirstError);
    }

    [Fact]
    public async Task InviteAsync_WithValidRequest_CreatesInvitationAndSendsEmail()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, inviterId, WorkspaceRole.Admin, DateTime.UtcNow);

        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, inviterId).Returns(admin);
        _identityService.FindUserIdByEmailAsync("new@example.com").Returns((Guid?)null);
        _invitationRepository.GetPendingByWorkspaceAndEmailAsync(workspace.Id, "new@example.com").Returns((WorkspaceInvitation?)null);

        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "New@Example.com", Role = WorkspaceRole.Admin });

        Assert.False(result.IsError);
        Assert.Equal("new@example.com", result.Value.Email);
        Assert.Equal(WorkspaceRole.Admin, result.Value.Role);
        Assert.Equal(InvitationStatus.Pending, result.Value.Status);
        await _invitationRepository.Received(1).AddAsync(Arg.Any<WorkspaceInvitation>());
        await _emailSender.Received(1).SendAsync("new@example.com", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetPendingForCurrentUserAsync_IncludesWorkspaceHasIcon()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([1, 2, 3], "image/png", DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspace.Id, "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);

        _invitationRepository.GetPendingForEmailAsync("invitee@example.com")
            .Returns(new List<WorkspaceInvitation> { invitation });
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);

        var result = await _sut.GetPendingForCurrentUserAsync(Guid.NewGuid(), "invitee@example.com");

        Assert.False(result.IsError);
        Assert.Single(result.Value);
        Assert.True(result.Value[0].WorkspaceHasIcon);
    }

    [Fact]
    public async Task GetPendingForCurrentUserAsync_WhenWorkspaceIsMissing_ReturnsHasIconFalse()
    {
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);

        _invitationRepository.GetPendingForEmailAsync("invitee@example.com")
            .Returns(new List<WorkspaceInvitation> { invitation });
        _workspaceRepository.GetByIdAsync(invitation.WorkspaceId).Returns((Workspace?)null);

        var result = await _sut.GetPendingForCurrentUserAsync(Guid.NewGuid(), "invitee@example.com");

        Assert.False(result.IsError);
        Assert.False(result.Value[0].WorkspaceHasIcon);
        Assert.Equal("Unknown workspace", result.Value[0].WorkspaceName);
    }

    [Fact]
    public async Task GetWorkspaceIconAsync_WhenEmailDoesNotMatch_ReturnsNotFound()
    {
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        var result = await _sut.GetWorkspaceIconAsync("someoneelse@example.com", invitation.Id);

        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetWorkspaceIconAsync_WhenWorkspaceHasNoIcon_ReturnsNotFound()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspace.Id, "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);

        var result = await _sut.GetWorkspaceIconAsync("invitee@example.com", invitation.Id);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.IconNotFound, result.FirstError);
    }

    [Fact]
    public async Task GetWorkspaceIconAsync_WhenValid_ReturnsIconBytes()
    {
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([9, 9, 9], "image/png", DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspace.Id, "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);

        var result = await _sut.GetWorkspaceIconAsync("invitee@example.com", invitation.Id);

        Assert.False(result.IsError);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Equal(new byte[] { 9, 9, 9 }, result.Value.Data);
    }

    [Fact]
    public async Task AcceptAsync_WhenEmailDoesNotMatch_ReturnsNotFound()
    {
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        var result = await _sut.AcceptAsync(Guid.NewGuid(), "someoneelse@example.com", invitation.Id);

        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task AcceptAsync_WhenPendingAndEmailMatches_AddsWorkspaceMemberWithInvitedRole()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspaceId, "invitee@example.com", WorkspaceRole.Admin, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(false);

        var result = await _sut.AcceptAsync(userId, "invitee@example.com", invitation.Id);

        Assert.False(result.IsError);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        await _invitationRepository.Received(1).UpdateAsync(invitation);
        await _workspaceMemberRepository.Received(1).AddAsync(
            Arg.Is<WorkspaceMember>(m => m.UserId == userId && m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin));
    }

    [Fact]
    public async Task AcceptAsync_WhenAlreadyResponded_ReturnsConflict()
    {
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        invitation.Decline(DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        var result = await _sut.AcceptAsync(Guid.NewGuid(), "invitee@example.com", invitation.Id);

        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotPending, result.FirstError);
    }

    [Fact]
    public async Task RevokeAsync_WhenNotAdmin_ReturnsForbidden()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns(member);

        var result = await _sut.RevokeAsync(userId, workspaceId, Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task RevokeAsync_WhenPendingAndOwnedByWorkspace_RevokesInvitation()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Admin, DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspaceId, "invitee@example.com", WorkspaceRole.Member, userId, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns(admin);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        var result = await _sut.RevokeAsync(userId, workspaceId, invitation.Id);

        Assert.False(result.IsError);
        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
        await _invitationRepository.Received(1).UpdateAsync(invitation);
    }
}
