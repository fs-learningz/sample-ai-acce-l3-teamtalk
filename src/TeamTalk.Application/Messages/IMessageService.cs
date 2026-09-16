using ErrorOr;
using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Application.Messages;

public interface IMessageService
{
    Task<ErrorOr<MessageResponse>> SendAsync(
        Guid userId,
        Guid channelId,
        SendMessageRequest request,
        CancellationToken cancellationToken);

    Task<ErrorOr<MessageResponse>> EditAsync(
        Guid userId,
        Guid channelId,
        Guid messageId,
        EditMessageRequest request,
        CancellationToken cancellationToken);

    Task<ErrorOr<Success>> DeleteAsync(
        Guid userId,
        Guid workspaceId,
        Guid channelId,
        Guid messageId,
        CancellationToken cancellationToken);

    Task<ErrorOr<ChannelMessagesResponse>> GetHistoryAsync(
        Guid userId,
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize,
        CancellationToken cancellationToken);
}
