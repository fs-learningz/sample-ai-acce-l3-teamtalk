using ErrorOr;
using TeamTalk.Application.Channels.Contracts;

namespace TeamTalk.Application.Channels;

public interface IChannelService
{
    Task<ErrorOr<ChannelResponse>> CreateAsync(Guid userId, Guid workspaceId, CreateChannelRequest request);

    Task<ErrorOr<IReadOnlyList<ChannelResponse>>> GetVisibleAsync(Guid userId, Guid workspaceId);

    Task<ErrorOr<ChannelDetailsResponse>> GetDetailsAsync(Guid userId, Guid workspaceId, Guid channelId);

    Task<ErrorOr<Success>> MarkReadAsync(Guid userId, Guid workspaceId, Guid channelId);

    Task<ErrorOr<Success>> UpdatePreferencesAsync(
        Guid userId,
        Guid workspaceId,
        Guid channelId,
        UpdateChannelPreferencesRequest request);

    Task<ErrorOr<Success>> LeaveAsync(Guid userId, Guid workspaceId, Guid channelId);

    Task<ErrorOr<Success>> DeleteAsync(Guid userId, Guid workspaceId, Guid channelId);

    Task<ErrorOr<Success>> JoinAsync(Guid userId, Guid workspaceId, Guid channelId);

    Task<ErrorOr<Success>> CanUserAccessChannelAsync(Guid channelId, Guid userId);

    Task<ErrorOr<Success>> EnsureCanPostAsync(Guid channelId, Guid userId);
}
