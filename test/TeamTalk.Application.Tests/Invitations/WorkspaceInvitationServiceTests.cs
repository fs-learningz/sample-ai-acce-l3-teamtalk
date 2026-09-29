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
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspace.Id, inviterId, WorkspaceRole.Member, DateTime.UtcNow);

        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, inviterId).Returns(member);

        // act
        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "new@example.com" });

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task InviteAsync_WhenTargetAlreadyAMember_ReturnsConflict()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, inviterId, WorkspaceRole.Admin, DateTime.UtcNow);
        var existingUserId = Guid.NewGuid();

        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, inviterId).Returns(admin);
        _identityService.FindUserIdByEmailAsync("existing@example.com").Returns(existingUserId);
        _workspaceMemberRepository.IsMemberAsync(workspace.Id, existingUserId).Returns(true);

        // act
        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "existing@example.com" });

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.AlreadyAMember, result.FirstError);
    }

    [Fact]
    public async Task InviteAsync_WhenPendingInvitationAlreadyExists_ReturnsConflict()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, inviterId, WorkspaceRole.Admin, DateTime.UtcNow);
        var existingInvitation = new WorkspaceInvitation(Guid.NewGuid(), workspace.Id, "new@example.com", WorkspaceRole.Member, inviterId, DateTime.UtcNow);

        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, inviterId).Returns(admin);
        _identityService.FindUserIdByEmailAsync("new@example.com").Returns((Guid?)null);
        _invitationRepository.GetPendingByWorkspaceAndEmailAsync(workspace.Id, "new@example.com").Returns(existingInvitation);

        // act
        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "new@example.com" });

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.AlreadyPending, result.FirstError);
    }

    [Fact]
    public async Task InviteAsync_WithValidRequest_CreatesInvitationAndSendsEmail()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspace.Id, inviterId, WorkspaceRole.Admin, DateTime.UtcNow);

        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, inviterId).Returns(admin);
        _identityService.FindUserIdByEmailAsync("new@example.com").Returns((Guid?)null);
        _invitationRepository.GetPendingByWorkspaceAndEmailAsync(workspace.Id, "new@example.com").Returns((WorkspaceInvitation?)null);

        // act
        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "New@Example.com", Role = WorkspaceRole.Admin });

        // assert
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
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([1, 2, 3], "image/png", DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspace.Id, "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);

        _invitationRepository.GetPendingForEmailAsync("invitee@example.com")
            .Returns(new List<WorkspaceInvitation> { invitation });
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);

        // act
        var result = await _sut.GetPendingForCurrentUserAsync(Guid.NewGuid(), "invitee@example.com");

        // assert
        Assert.False(result.IsError);
        Assert.Single(result.Value);
        Assert.True(result.Value[0].WorkspaceHasIcon);
    }

    [Fact]
    public async Task GetPendingForCurrentUserAsync_WhenWorkspaceIsMissing_ReturnsHasIconFalse()
    {
        // arrange
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);

        _invitationRepository.GetPendingForEmailAsync("invitee@example.com")
            .Returns(new List<WorkspaceInvitation> { invitation });
        _workspaceRepository.GetByIdAsync(invitation.WorkspaceId).Returns((Workspace?)null);

        // act
        var result = await _sut.GetPendingForCurrentUserAsync(Guid.NewGuid(), "invitee@example.com");

        // assert
        Assert.False(result.IsError);
        Assert.False(result.Value[0].WorkspaceHasIcon);
        Assert.Equal("Unknown workspace", result.Value[0].WorkspaceName);
    }

    [Fact]
    public async Task GetWorkspaceIconAsync_WhenEmailDoesNotMatch_ReturnsNotFound()
    {
        // arrange
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.GetWorkspaceIconAsync("someoneelse@example.com", invitation.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetWorkspaceIconAsync_WhenWorkspaceHasNoIcon_ReturnsNotFound()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspace.Id, "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);

        // act
        var result = await _sut.GetWorkspaceIconAsync("invitee@example.com", invitation.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.IconNotFound, result.FirstError);
    }

    [Fact]
    public async Task GetWorkspaceIconAsync_WhenValid_ReturnsIconBytes()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([9, 9, 9], "image/png", DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspace.Id, "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);

        // act
        var result = await _sut.GetWorkspaceIconAsync("invitee@example.com", invitation.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Equal(new byte[] { 9, 9, 9 }, result.Value.Data);
    }

    [Fact]
    public async Task AcceptAsync_WhenEmailDoesNotMatch_ReturnsNotFound()
    {
        // arrange
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.AcceptAsync(Guid.NewGuid(), "someoneelse@example.com", invitation.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task AcceptAsync_WhenPendingAndEmailMatches_AddsWorkspaceMemberWithInvitedRole()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspaceId, "invitee@example.com", WorkspaceRole.Admin, Guid.NewGuid(), DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(false);

        // act
        var result = await _sut.AcceptAsync(userId, "invitee@example.com", invitation.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        await _invitationRepository.Received(1).UpdateAsync(invitation);
        await _workspaceMemberRepository.Received(1).AddAsync(
            Arg.Is<WorkspaceMember>(m => m.UserId == userId && m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin));
    }

    [Fact]
    public async Task AcceptAsync_WhenAlreadyResponded_ReturnsConflict()
    {
        // arrange
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "invitee@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
        invitation.Decline(DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.AcceptAsync(Guid.NewGuid(), "invitee@example.com", invitation.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotPending, result.FirstError);
    }

    [Fact]
    public async Task RevokeAsync_WhenNotAdmin_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var member = new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns(member);

        // act
        var result = await _sut.RevokeAsync(userId, workspaceId, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task RevokeAsync_WhenPendingAndOwnedByWorkspace_RevokesInvitation()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Admin, DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.NewGuid(), workspaceId, "invitee@example.com", WorkspaceRole.Member, userId, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns(admin);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.RevokeAsync(userId, workspaceId, invitation.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
        await _invitationRepository.Received(1).UpdateAsync(invitation);
    }

    private static WorkspaceInvitation CreateInvitation(
        Guid workspaceId,
        string email = "invitee@example.com",
        WorkspaceRole role = WorkspaceRole.Member)
    {
        return new WorkspaceInvitation(Guid.NewGuid(), workspaceId, email, role, Guid.NewGuid(), DateTime.UtcNow);
    }

    private void ArrangeAdmin(Workspace workspace, Guid userId)
    {
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Admin, DateTime.UtcNow));
    }

    [Fact]
    public async Task InviteAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        // act
        var result = await _sut.InviteAsync(Guid.NewGuid(), Guid.NewGuid(), new InviteMemberRequest { Email = "new@example.com" });

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task InviteAsync_WhenInviterIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.InviteAsync(Guid.NewGuid(), workspace.Id, new InviteMemberRequest { Email = "new@example.com" });

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public async Task InviteAsync_WhenEmailIsInvalid_ReturnsValidationError(string email)
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        ArrangeAdmin(workspace, inviterId);

        // act
        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = email });

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Invitation.EmailRequired", result.FirstError.Code);
        await _invitationRepository.DidNotReceive().AddAsync(Arg.Any<WorkspaceInvitation>());
    }

    [Fact]
    public async Task InviteAsync_WhenEmailHasMixedCaseAndWhitespace_NormalizesBeforeLookupsAndPersisting()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        ArrangeAdmin(workspace, inviterId);

        // act
        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "  New.User@Example.COM  " });

        // assert
        Assert.False(result.IsError);
        Assert.Equal("new.user@example.com", result.Value.Email);
        await _identityService.Received(1).FindUserIdByEmailAsync("new.user@example.com");
        await _invitationRepository.Received(1).GetPendingByWorkspaceAndEmailAsync(workspace.Id, "new.user@example.com");
    }

    [Fact]
    public async Task InviteAsync_WhenExistingUserIsNotYetAMember_CreatesInvitation()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        var existingUserId = Guid.NewGuid();
        ArrangeAdmin(workspace, inviterId);
        _identityService.FindUserIdByEmailAsync("new@example.com").Returns(existingUserId);
        _workspaceMemberRepository.IsMemberAsync(workspace.Id, existingUserId).Returns(false);

        // act
        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "new@example.com", Role = WorkspaceRole.Admin });

        // assert
        Assert.False(result.IsError);
        Assert.Equal(WorkspaceRole.Admin, result.Value.Role);
        await _invitationRepository.Received(1).AddAsync(Arg.Is<WorkspaceInvitation>(i => i.Email == "new@example.com"));
    }

    [Fact]
    public async Task InviteAsync_WhenEmailSenderThrows_StillReturnsInvitation()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var inviterId = Guid.NewGuid();
        ArrangeAdmin(workspace, inviterId);
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("smtp down")));

        // act
        var result = await _sut.InviteAsync(inviterId, workspace.Id, new InviteMemberRequest { Email = "new@example.com" });

        // assert
        Assert.False(result.IsError);
        await _invitationRepository.Received(1).AddAsync(Arg.Any<WorkspaceInvitation>());
    }

    [Fact]
    public async Task GetForWorkspaceAsync_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _workspaceRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Workspace?)null);

        // act
        var result = await _sut.GetForWorkspaceAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetForWorkspaceAsync_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.GetForWorkspaceAsync(Guid.NewGuid(), workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task GetForWorkspaceAsync_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _workspaceMemberRepository.GetAsync(workspace.Id, userId)
            .Returns(new WorkspaceMember(Guid.NewGuid(), workspace.Id, userId, WorkspaceRole.Member, DateTime.UtcNow));

        // act
        var result = await _sut.GetForWorkspaceAsync(userId, workspace.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
    }

    [Fact]
    public async Task GetForWorkspaceAsync_WhenAdmin_ReturnsInvitationsWithWorkspaceName()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        ArrangeAdmin(workspace, userId);
        var invitation = CreateInvitation(workspace.Id);
        _invitationRepository.GetForWorkspaceAsync(workspace.Id).Returns(new List<WorkspaceInvitation> { invitation });

        // act
        var result = await _sut.GetForWorkspaceAsync(userId, workspace.Id);

        // assert
        Assert.False(result.IsError);
        var single = Assert.Single(result.Value);
        Assert.Equal(invitation.Id, single.Id);
        Assert.Equal("Engineering", single.WorkspaceName);
    }

    [Fact]
    public async Task GetPendingForCurrentUserAsync_WhenNoInvitations_ReturnsEmptyList()
    {
        // arrange
        _invitationRepository.GetPendingForEmailAsync(Arg.Any<string>()).Returns(new List<WorkspaceInvitation>());

        // act
        var result = await _sut.GetPendingForCurrentUserAsync(Guid.NewGuid(), "invitee@example.com");

        // assert
        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetPendingForCurrentUserAsync_NormalizesEmailAndLoadsEachWorkspaceOnce()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        _workspaceRepository.GetByIdAsync(workspace.Id).Returns(workspace);
        _invitationRepository.GetPendingForEmailAsync("invitee@example.com").Returns(new List<WorkspaceInvitation>
        {
            CreateInvitation(workspace.Id),
            CreateInvitation(workspace.Id),
        });

        // act
        var result = await _sut.GetPendingForCurrentUserAsync(Guid.NewGuid(), "  Invitee@Example.com ");

        // assert
        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Count);
        await _workspaceRepository.Received(1).GetByIdAsync(workspace.Id);
    }

    [Fact]
    public async Task DeclineAsync_WhenPendingAndEmailMatches_DeclinesInvitation()
    {
        // arrange
        var invitation = CreateInvitation(Guid.NewGuid());
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.DeclineAsync(Guid.NewGuid(), "invitee@example.com", invitation.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(InvitationStatus.Declined, invitation.Status);
        await _invitationRepository.Received(1).UpdateAsync(invitation);
    }

    [Fact]
    public async Task DeclineAsync_WhenEmailDoesNotMatch_ReturnsNotFound()
    {
        // arrange
        var invitation = CreateInvitation(Guid.NewGuid());
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.DeclineAsync(Guid.NewGuid(), "someone.else@example.com", invitation.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotFound, result.FirstError);
        await _invitationRepository.DidNotReceive().UpdateAsync(Arg.Any<WorkspaceInvitation>());
    }

    [Fact]
    public async Task DeclineAsync_WhenAlreadyResponded_ReturnsNotPending()
    {
        // arrange
        var invitation = CreateInvitation(Guid.NewGuid());
        invitation.Decline(DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.DeclineAsync(Guid.NewGuid(), "invitee@example.com", invitation.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotPending, result.FirstError);
    }

    [Fact]
    public async Task AcceptAsync_WhenUserIsAlreadyAMember_DoesNotAddDuplicateMember()
    {
        // arrange
        var userId = Guid.NewGuid();
        var invitation = CreateInvitation(Guid.NewGuid());
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);
        _workspaceMemberRepository.IsMemberAsync(invitation.WorkspaceId, userId).Returns(true);

        // act
        var result = await _sut.AcceptAsync(userId, "invitee@example.com", invitation.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        await _workspaceMemberRepository.DidNotReceive().AddAsync(Arg.Any<WorkspaceMember>());
    }

    [Fact]
    public async Task AcceptAsync_WhenInvitationDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _invitationRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((WorkspaceInvitation?)null);

        // act
        var result = await _sut.AcceptAsync(Guid.NewGuid(), "invitee@example.com", Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task RevokeAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        _workspaceMemberRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.RevokeAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task RevokeAsync_WhenInvitationBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        ArrangeAdmin(workspace, userId);
        var invitation = CreateInvitation(Guid.NewGuid());
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.RevokeAsync(userId, workspace.Id, invitation.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task RevokeAsync_WhenInvitationIsNotPending_ReturnsNotPending()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var userId = Guid.NewGuid();
        ArrangeAdmin(workspace, userId);
        var invitation = CreateInvitation(workspace.Id);
        invitation.Accept(DateTime.UtcNow);
        _invitationRepository.GetByIdAsync(invitation.Id).Returns(invitation);

        // act
        var result = await _sut.RevokeAsync(userId, workspace.Id, invitation.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(InvitationErrors.NotPending, result.FirstError);
        await _invitationRepository.DidNotReceive().UpdateAsync(Arg.Any<WorkspaceInvitation>());
    }
}
