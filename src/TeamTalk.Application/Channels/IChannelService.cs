using ErrorOr;
using TeamTalk.Application.Channels.Contracts;

namespace TeamTalk.Application.Channels;

public interface IChannelService
{
    Task<ErrorOr<ChannelResponse>> CreateAsync(Guid userId, Guid workspaceId, CreateChannelRequest request, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<ChannelResponse>>> GetVisibleAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> JoinAsync(Guid userId, Guid workspaceId, Guid channelId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> CanUserAccessChannelAsync(Guid channelId, Guid userId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> EnsureCanPostAsync(Guid channelId, Guid userId, CancellationToken cancellationToken);
}
