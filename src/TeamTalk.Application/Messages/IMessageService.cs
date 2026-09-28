using ErrorOr;
using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Application.Messages;

public interface IMessageService
{
    Task<ErrorOr<MessageResponse>> SendAsync(
        Guid userId,
        Guid channelId,
        SendMessageRequest request);

    Task<ErrorOr<MessageResponse>> EditAsync(
        Guid userId,
        Guid channelId,
        Guid messageId,
        EditMessageRequest request);

    Task<ErrorOr<Success>> DeleteAsync(
        Guid userId,
        Guid workspaceId,
        Guid channelId,
        Guid messageId);

    Task<ErrorOr<ChannelMessagesResponse>> GetHistoryAsync(
        Guid userId,
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize);
}
