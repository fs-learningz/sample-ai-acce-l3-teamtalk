using TeamTalk.Domain.Common;
using TeamTalk.Domain.Enums;

namespace TeamTalk.Domain.Entities;

public class WorkspaceInvitation
{
    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public string Email { get; private set; }

    public WorkspaceRole Role { get; private set; }

    public Guid InvitedByUserId { get; private set; }

    public InvitationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RespondedAtUtc { get; private set; }

    public WorkspaceInvitation(
        Guid id,
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        Guid invitedByUserId,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainException("A valid email address is required.");
        }

        Id = id;
        WorkspaceId = workspaceId;
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        InvitedByUserId = invitedByUserId;
        Status = InvitationStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    private WorkspaceInvitation()
    {
        Email = string.Empty;
    }

    public void Accept(DateTime respondedAtUtc)
    {
        EnsurePending();
        Status = InvitationStatus.Accepted;
        RespondedAtUtc = respondedAtUtc;
    }

    public void Decline(DateTime respondedAtUtc)
    {
        EnsurePending();
        Status = InvitationStatus.Declined;
        RespondedAtUtc = respondedAtUtc;
    }

    public void Revoke(DateTime respondedAtUtc)
    {
        EnsurePending();
        Status = InvitationStatus.Revoked;
        RespondedAtUtc = respondedAtUtc;
    }

    private void EnsurePending()
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new DomainException("This invitation has already been responded to.");
        }
    }
}
