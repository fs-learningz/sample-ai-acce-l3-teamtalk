using ErrorOr;
using TeamTalk.Application.Channels.Contracts;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Channels;

public class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelMemberRepository _channelMemberRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IChannelUserStateRepository _channelUserStateRepository;
    private readonly IChannelQueryService _channelQueryService;

    public ChannelService(
        IChannelRepository channelRepository,
        IChannelMemberRepository channelMemberRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IChannelUserStateRepository channelUserStateRepository,
        IChannelQueryService channelQueryService)
    {
        _channelUserStateRepository = channelUserStateRepository;
        _channelQueryService = channelQueryService;
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

        var channels = await _channelQueryService.GetVisibleAsync(workspaceId, userId);

        return channels.ToList();
    }

    public async Task<ErrorOr<ChannelDetailsResponse>> GetDetailsAsync(Guid userId, Guid workspaceId, Guid channelId)
    {
        var access = await GetAccessibleChannelAsync(userId, workspaceId, channelId);

        if (access.IsError)
        {
            return access.FirstError;
        }

        var details = await _channelQueryService.GetDetailsAsync(channelId, userId);

        if (details is null)
        {
            return ChannelErrors.NotFound;
        }

        return details;
    }

    public async Task<ErrorOr<Success>> MarkReadAsync(Guid userId, Guid workspaceId, Guid channelId)
    {
        var access = await GetAccessibleChannelAsync(userId, workspaceId, channelId);

        if (access.IsError)
        {
            return access.FirstError;
        }

        var nowUtc = DateTime.UtcNow;
        var state = await _channelUserStateRepository.GetAsync(channelId, userId);

        if (state is null)
        {
            state = new ChannelUserState(Guid.CreateVersion7(), channelId, userId);
            state.MarkRead(nowUtc);
            await _channelUserStateRepository.AddAsync(state);

            return Result.Success;
        }

        state.MarkRead(nowUtc);
        await _channelUserStateRepository.UpdateAsync(state);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdatePreferencesAsync(
        Guid userId,
        Guid workspaceId,
        Guid channelId,
        UpdateChannelPreferencesRequest request)
    {
        var access = await GetAccessibleChannelAsync(userId, workspaceId, channelId);

        if (access.IsError)
        {
            return access.FirstError;
        }

        var state = await _channelUserStateRepository.GetAsync(channelId, userId);

        if (state is null)
        {
            state = new ChannelUserState(Guid.CreateVersion7(), channelId, userId);
            state.SetPreferences(request.IsPinned, request.IsMuted);
            await _channelUserStateRepository.AddAsync(state);

            return Result.Success;
        }

        state.SetPreferences(request.IsPinned, request.IsMuted);
        await _channelUserStateRepository.UpdateAsync(state);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> LeaveAsync(Guid userId, Guid workspaceId, Guid channelId)
    {
        var access = await GetAccessibleChannelAsync(userId, workspaceId, channelId);

        if (access.IsError)
        {
            return access.FirstError;
        }

        var isChannelMember = await _channelMemberRepository.IsMemberAsync(channelId, userId);

        if (isChannelMember is false)
        {
            return ChannelErrors.NotAMember;
        }

        await _channelMemberRepository.RemoveAsync(channelId, userId);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid userId, Guid workspaceId, Guid channelId)
    {
        var workspaceMember = await _workspaceMemberRepository.GetAsync(workspaceId, userId);

        if (workspaceMember is null)
        {
            return WorkspaceErrors.NotAMember;
        }

        var channel = await _channelRepository.GetByIdAsync(channelId);

        if (channel is null || channel.WorkspaceId != workspaceId)
        {
            return ChannelErrors.NotFound;
        }

        if (workspaceMember.IsAdmin() is false)
        {
            return WorkspaceErrors.NotAnAdmin;
        }

        await _channelRepository.DeleteAsync(channelId);

        return Result.Success;
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

    private async Task<ErrorOr<Channel>> GetAccessibleChannelAsync(Guid userId, Guid workspaceId, Guid channelId)
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
            var isChannelMember = await _channelMemberRepository.IsMemberAsync(channelId, userId);

            if (isChannelMember is false)
            {
                return ChannelErrors.NotVisible;
            }
        }

        return channel;
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
