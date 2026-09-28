using ErrorOr;
using Microsoft.Extensions.Logging;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Invitations.Contracts;
using TeamTalk.Application.Workspaces.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Invitations;

public class WorkspaceInvitationService : IWorkspaceInvitationService
{
    private readonly IWorkspaceInvitationRepository _invitationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IIdentityService _identityService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<WorkspaceInvitationService> _logger;

    public WorkspaceInvitationService(
        IWorkspaceInvitationRepository invitationRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IIdentityService identityService,
        IEmailSender emailSender,
        ILogger<WorkspaceInvitationService> logger)
    {
        _invitationRepository = invitationRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _identityService = identityService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<ErrorOr<WorkspaceInvitationResponse>> InviteAsync(
        Guid inviterUserId,
        Guid workspaceId,
        InviteMemberRequest request)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var inviter = await _workspaceMemberRepository.GetAsync(workspaceId, inviterUserId);

        if (inviter is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (inviter.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Contains('@') is false)
        {
            return Error.Validation("Invitation.EmailRequired", "A valid email address is required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUserId = await _identityService.FindUserIdByEmailAsync(normalizedEmail);

        if (existingUserId is not null)
        {
            var isAlreadyMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, existingUserId.Value);

            if (isAlreadyMember)
            {
                return InvitationErrors.AlreadyAMember;
            }
        }

        var existingInvitation = await _invitationRepository.GetPendingByWorkspaceAndEmailAsync(workspaceId, normalizedEmail);

        if (existingInvitation is not null)
        {
            return InvitationErrors.AlreadyPending;
        }

        var nowUtc = DateTime.UtcNow;
        var invitation = new WorkspaceInvitation(
            Guid.CreateVersion7(),
            workspaceId,
            normalizedEmail,
            request.Role,
            inviterUserId,
            nowUtc);

        await _invitationRepository.AddAsync(invitation);

        await TrySendInvitationEmailAsync(invitation, workspace.Name);

        return ToResponse(invitation, workspace);
    }

    public async Task<ErrorOr<IReadOnlyList<WorkspaceInvitationResponse>>> GetForWorkspaceAsync(
        Guid userId,
        Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId);

        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (member.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        var invitations = await _invitationRepository.GetForWorkspaceAsync(workspaceId);

        return invitations.Select(i => ToResponse(i, workspace)).ToList();
    }

    public async Task<ErrorOr<IReadOnlyList<WorkspaceInvitationResponse>>> GetPendingForCurrentUserAsync(
        Guid userId,
        string userEmail)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        var invitations = await _invitationRepository.GetPendingForEmailAsync(normalizedEmail);

        var workspaceCache = new Dictionary<Guid, Workspace?>();
        var responses = new List<WorkspaceInvitationResponse>();

        foreach (var invitation in invitations)
        {
            if (workspaceCache.TryGetValue(invitation.WorkspaceId, out var workspace) is false)
            {
                workspace = await _workspaceRepository.GetByIdAsync(invitation.WorkspaceId);
                workspaceCache[invitation.WorkspaceId] = workspace;
            }

            responses.Add(ToResponse(invitation, workspace));
        }

        return responses;
    }

    public async Task<ErrorOr<WorkspaceIconResponse>> GetWorkspaceIconAsync(
        string userEmail,
        Guid invitationId)
    {
        var invitationResult = await GetPendingInvitationForUserAsync(userEmail, invitationId);

        if (invitationResult.IsError)
        {
            return invitationResult.Errors;
        }

        var invitation = invitationResult.Value;
        var workspace = await _workspaceRepository.GetByIdAsync(invitation.WorkspaceId);

        if (workspace?.IconData is null || workspace.IconContentType is null)
        {
            return WorkspaceErrors.IconNotFound;
        }

        return new WorkspaceIconResponse { Data = workspace.IconData, ContentType = workspace.IconContentType };
    }

    public async Task<ErrorOr<Success>> AcceptAsync(Guid userId, string userEmail, Guid invitationId)
    {
        var invitationResult = await GetPendingInvitationForUserAsync(userEmail, invitationId);

        if (invitationResult.IsError)
        {
            return invitationResult.Errors;
        }

        var invitation = invitationResult.Value;
        var nowUtc = DateTime.UtcNow;
        invitation.Accept(nowUtc);
        await _invitationRepository.UpdateAsync(invitation);

        var isAlreadyMember = await _workspaceMemberRepository.IsMemberAsync(invitation.WorkspaceId, userId);

        if (isAlreadyMember is false)
        {
            var member = new WorkspaceMember(Guid.CreateVersion7(), invitation.WorkspaceId, userId, invitation.Role, nowUtc);
            await _workspaceMemberRepository.AddAsync(member);
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> DeclineAsync(Guid userId, string userEmail, Guid invitationId)
    {
        var invitationResult = await GetPendingInvitationForUserAsync(userEmail, invitationId);

        if (invitationResult.IsError)
        {
            return invitationResult.Errors;
        }

        var invitation = invitationResult.Value;
        invitation.Decline(DateTime.UtcNow);
        await _invitationRepository.UpdateAsync(invitation);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> RevokeAsync(Guid userId, Guid workspaceId, Guid invitationId)
    {
        var member = await _workspaceMemberRepository.GetAsync(workspaceId, userId);

        if (member is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (member.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        var invitation = await _invitationRepository.GetByIdAsync(invitationId);

        if (invitation is null || invitation.WorkspaceId != workspaceId)
        {
            return InvitationErrors.NotFound;
        }

        if (invitation.Status is not InvitationStatus.Pending)
        {
            return InvitationErrors.NotPending;
        }

        invitation.Revoke(DateTime.UtcNow);
        await _invitationRepository.UpdateAsync(invitation);

        return Result.Success;
    }

    private async Task<ErrorOr<WorkspaceInvitation>> GetPendingInvitationForUserAsync(
        string userEmail,
        Guid invitationId)
    {
        var invitation = await _invitationRepository.GetByIdAsync(invitationId);

        if (invitation is null || string.Equals(invitation.Email, userEmail.Trim().ToLowerInvariant(), StringComparison.Ordinal) is false)
        {
            return InvitationErrors.NotFound;
        }

        if (invitation.Status is not InvitationStatus.Pending)
        {
            return InvitationErrors.NotPending;
        }

        return invitation;
    }

    private async Task TrySendInvitationEmailAsync(WorkspaceInvitation invitation, string workspaceName)
    {
        try
        {
            var subject = $"You've been invited to join {workspaceName} on TeamTalk";
            var body = $"<p>You've been invited to join <strong>{workspaceName}</strong> on TeamTalk.</p>" +
                       "<p>Log in to TeamTalk with this email address to view and respond to the invitation.</p>";

            await _emailSender.SendAsync(invitation.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send invitation email for invitation {InvitationId}", invitation.Id);
        }
    }

    private static WorkspaceInvitationResponse ToResponse(WorkspaceInvitation invitation, Workspace? workspace)
    {
        return new WorkspaceInvitationResponse
        {
            Id = invitation.Id,
            WorkspaceId = invitation.WorkspaceId,
            WorkspaceName = workspace?.Name ?? "Unknown workspace",
            WorkspaceHasIcon = workspace?.HasIcon ?? false,
            Email = invitation.Email,
            Role = invitation.Role,
            Status = invitation.Status,
            InvitedByUserId = invitation.InvitedByUserId,
            CreatedAtUtc = invitation.CreatedAtUtc,
        };
    }
}
