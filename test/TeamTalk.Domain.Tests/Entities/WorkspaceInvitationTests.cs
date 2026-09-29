using TeamTalk.Domain.Common;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;

namespace TeamTalk.Domain.Tests.Entities;

public class WorkspaceInvitationTests
{
    [Fact]
    public void Constructor_WithValidEmail_SetsPropertiesAndNormalizesEmail()
    {
        // arrange
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;

        // act
        var invitation = new WorkspaceInvitation(id, workspaceId, "  Someone@Example.com  ", WorkspaceRole.Admin, invitedByUserId, createdAtUtc);

        // assert
        Assert.Equal(id, invitation.Id);
        Assert.Equal(workspaceId, invitation.WorkspaceId);
        Assert.Equal("someone@example.com", invitation.Email);
        Assert.Equal(WorkspaceRole.Admin, invitation.Role);
        Assert.Equal(invitedByUserId, invitation.InvitedByUserId);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Null(invitation.RespondedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Constructor_WithInvalidEmail_Throws(string email)
    {
        // act & assert
        Assert.Throws<DomainException>(() =>
            new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), email, WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Accept_WhenPending_SetsAcceptedStatus()
    {
        // arrange
        var invitation = CreatePendingInvitation();
        var respondedAtUtc = DateTime.UtcNow;

        // act
        invitation.Accept(respondedAtUtc);

        // assert
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(respondedAtUtc, invitation.RespondedAtUtc);
    }

    [Fact]
    public void Decline_WhenPending_SetsDeclinedStatus()
    {
        // arrange
        var invitation = CreatePendingInvitation();
        var respondedAtUtc = DateTime.UtcNow;

        // act
        invitation.Decline(respondedAtUtc);

        // assert
        Assert.Equal(InvitationStatus.Declined, invitation.Status);
        Assert.Equal(respondedAtUtc, invitation.RespondedAtUtc);
    }

    [Fact]
    public void Revoke_WhenPending_SetsRevokedStatus()
    {
        // arrange
        var invitation = CreatePendingInvitation();
        var respondedAtUtc = DateTime.UtcNow;

        // act
        invitation.Revoke(respondedAtUtc);

        // assert
        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
        Assert.Equal(respondedAtUtc, invitation.RespondedAtUtc);
    }

    [Fact]
    public void Accept_WhenAlreadyResponded_Throws()
    {
        // arrange
        var invitation = CreatePendingInvitation();
        invitation.Accept(DateTime.UtcNow);

        // act & assert
        Assert.Throws<DomainException>(() => invitation.Accept(DateTime.UtcNow));
    }

    [Fact]
    public void Decline_WhenAlreadyRevoked_Throws()
    {
        // arrange
        var invitation = CreatePendingInvitation();
        invitation.Revoke(DateTime.UtcNow);

        // act & assert
        Assert.Throws<DomainException>(() => invitation.Decline(DateTime.UtcNow));
    }

    private static WorkspaceInvitation CreatePendingInvitation()
    {
        return new WorkspaceInvitation(Guid.NewGuid(), Guid.NewGuid(), "someone@example.com", WorkspaceRole.Member, Guid.NewGuid(), DateTime.UtcNow);
    }
}
