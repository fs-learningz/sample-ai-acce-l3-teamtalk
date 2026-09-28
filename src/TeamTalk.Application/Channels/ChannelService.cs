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
        CreateChannelRequest request)
    {
        var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, userId);

        if (isWorkspaceMember is false)
        {
            return WorkspaceErrors.NotAMember;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Error.Validation("Channel.NameRequired", "Channel name is required.");
        }

        var nameExists = await _channelRepository.NameExistsInWorkspaceAsync(workspaceId, request.Name);

        if (nameExists)
        {
            return ChannelErrors.NameAlreadyExists;
        }

        var nowUtc = DateTime.UtcNow;
        var channel = new Channel(Guid.CreateVersion7(), workspaceId, request.Name, request.Type, userId, nowUtc);
        await _channelRepository.AddAsync(channel);

        var member = new ChannelMember(Guid.CreateVersion7(), channel.Id, userId, nowUtc);
        await _channelMemberRepository.AddAsync(member);

        return ToResponse(channel);
    }

    public async Task<ErrorOr<IReadOnlyList<ChannelResponse>>> GetVisibleAsync(
        Guid userId,
        Guid workspaceId)
    {
        var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, userId);

        if (isWorkspaceMember is false)
        {
            return WorkspaceErrors.NotAMember;
        }

        var channels = await _channelRepository.GetVisibleToUserAsync(workspaceId, userId);

        return channels.Select(ToResponse).ToList();
    }

    public async Task<ErrorOr<Success>> JoinAsync(Guid userId, Guid workspaceId, Guid channelId)
    {
        var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(workspaceId, userId);

        if (isWorkspaceMember is false)
        {
            return WorkspaceErrors.NotAMember;
        }

        var channel = await _channelRepository.GetByIdAsync(channelId);

        if (channel is null || channel.WorkspaceId != workspaceId)
        {
            return ChannelErrors.NotFound;
        }

        if (channel.IsPublic() is false)
        {
            return ChannelErrors.CannotJoinPrivateChannel;
        }

        var isChannelMember = await _channelMemberRepository.IsMemberAsync(channelId, userId);

        if (isChannelMember is false)
        {
            var member = new ChannelMember(Guid.CreateVersion7(), channelId, userId, DateTime.UtcNow);
            await _channelMemberRepository.AddAsync(member);
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> CanUserAccessChannelAsync(Guid channelId, Guid userId)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);

        if (channel is null)
        {
            return ChannelErrors.NotFound;
        }

        if (channel.IsPublic())
        {
            var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(channel.WorkspaceId, userId);

            if (isWorkspaceMember is false)
            {
                return WorkspaceErrors.NotAMember;
            }

            return Result.Success;
        }

        var isChannelMember = await _channelMemberRepository.IsMemberAsync(channelId, userId);

        if (isChannelMember is false)
        {
            return ChannelErrors.NotVisible;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> EnsureCanPostAsync(Guid channelId, Guid userId)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);

        if (channel is null)
        {
            return ChannelErrors.NotFound;
        }

        if (channel.IsPublic())
        {
            var isWorkspaceMember = await _workspaceMemberRepository.IsMemberAsync(channel.WorkspaceId, userId);

            if (isWorkspaceMember is false)
            {
                return WorkspaceErrors.NotAMember;
            }

            var isChannelMember = await _channelMemberRepository.IsMemberAsync(channelId, userId);

            if (isChannelMember is false)
            {
                var member = new ChannelMember(Guid.CreateVersion7(), channelId, userId, DateTime.UtcNow);
                await _channelMemberRepository.AddAsync(member);
            }

            return Result.Success;
        }

        var isPrivateMember = await _channelMemberRepository.IsMemberAsync(channelId, userId);

        if (isPrivateMember is false)
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
