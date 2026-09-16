using ErrorOr;
using TeamTalk.Application.Channels.Contracts;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Channels;

public class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelMemberRepository _channelMemberRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public ChannelService(
        IChannelRepository channelRepository,
        IChannelMemberRepository channelMemberRepository,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _channelRepository = channelRepository;
        _channelMemberRepository = channelMemberRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task<ErrorOr<ChannelResponse>> CreateAsync(
        Guid userId,
        Guid workspaceId,
        CreateChannelRequest request,
        CancellationToken cancellationToken)
    {
        var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, userId, cancellationToken);
        if (!isWorkspaceMember)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Error.Validation("Channel.NameRequired", "Channel name is required.");
        }

        var nameExists = await _channelRepository.NameExistsInWorkspaceAsync(workspaceId, request.Name, cancellationToken);
        if (nameExists)
        {
            return ChannelErrors.NameAlreadyExists;
        }

        var nowUtc = DateTime.UtcNow;
        var channel = new Channel(Guid.CreateVersion7(), workspaceId, request.Name, request.Type, userId, nowUtc);
        await _channelRepository.AddAsync(channel, cancellationToken);

        var member = new ChannelMember(Guid.CreateVersion7(), channel.Id, userId, nowUtc);
        await _channelMemberRepository.AddAsync(member, cancellationToken);

        return ToResponse(channel);
    }

    public async Task<ErrorOr<IReadOnlyList<ChannelResponse>>> GetVisibleAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, userId, cancellationToken);
        if (!isWorkspaceMember)
        {
            return WorkspaceErrors.NotAMember;
        }

        var channels = await _channelRepository.GetVisibleToUserAsync(workspaceId, userId, cancellationToken);
        return channels.Select(ToResponse).ToList();
    }

    public async Task<ErrorOr<Success>> JoinAsync(Guid userId, Guid workspaceId, Guid channelId, CancellationToken cancellationToken)
    {
        var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, userId, cancellationToken);
        if (!isWorkspaceMember)
        {
            return WorkspaceErrors.NotAMember;
        }

        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);
        if (channel is null || channel.WorkspaceId != workspaceId)
        {
            return ChannelErrors.NotFound;
        }

        if (!channel.IsPublic())
        {
            return ChannelErrors.CannotJoinPrivateChannel;
        }

        var isChannelMember = await _channelMemberRepository.IsMemberAsync(channelId, userId, cancellationToken);
        if (!isChannelMember)
        {
            var member = new ChannelMember(Guid.CreateVersion7(), channelId, userId, DateTime.UtcNow);
            await _channelMemberRepository.AddAsync(member, cancellationToken);
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> CanUserAccessChannelAsync(Guid channelId, Guid userId, CancellationToken cancellationToken)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);
        if (channel is null)
        {
            return ChannelErrors.NotFound;
        }

        if (channel.IsPublic())
        {
            var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(channel.WorkspaceId, userId, cancellationToken);
            if (!isWorkspaceMember)
            {
                return WorkspaceErrors.NotAMember;
            }

            return Result.Success;
        }

        var isChannelMember = await _channelMemberRepository.IsMemberAsync(channelId, userId, cancellationToken);
        if (!isChannelMember)
        {
            return ChannelErrors.NotVisible;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> EnsureCanPostAsync(Guid channelId, Guid userId, CancellationToken cancellationToken)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);
        if (channel is null)
        {
            return ChannelErrors.NotFound;
        }

        if (channel.IsPublic())
        {
            var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(channel.WorkspaceId, userId, cancellationToken);
            if (!isWorkspaceMember)
            {
                return WorkspaceErrors.NotAMember;
            }

            var isChannelMember = await _channelMemberRepository.IsMemberAsync(channelId, userId, cancellationToken);
            if (!isChannelMember)
            {
                var member = new ChannelMember(Guid.CreateVersion7(), channelId, userId, DateTime.UtcNow);
                await _channelMemberRepository.AddAsync(member, cancellationToken);
            }

            return Result.Success;
        }

        var isPrivateMember = await _channelMemberRepository.IsMemberAsync(channelId, userId, cancellationToken);
        if (!isPrivateMember)
        {
            return ChannelErrors.NotVisible;
        }

        return Result.Success;
    }

    private static ChannelResponse ToResponse(Channel channel)
    {
        return new ChannelResponse
        {
            Id = channel.Id,
            WorkspaceId = channel.WorkspaceId,
            Name = channel.Name,
            Type = channel.Type,
            CreatedByUserId = channel.CreatedByUserId,
            CreatedAtUtc = channel.CreatedAtUtc,
        };
    }
}
