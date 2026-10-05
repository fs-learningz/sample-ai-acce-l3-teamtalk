using TeamTalk.Application.Channels.Contracts;

namespace TeamTalk.Application.Common.Interfaces;

public interface IChannelQueryService
{
    /// <summary>
    /// Channels the user can see, with the user's pin/mute/unread state. Pinned channels come first.
    /// </summary>
    Task<IReadOnlyList<ChannelResponse>> GetVisibleAsync(Guid workspaceId, Guid userId);

    Task<ChannelDetailsResponse?> GetDetailsAsync(Guid channelId, Guid userId);
}
