using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Application.Common.Interfaces;

public interface IMessageQueryService
{
    Task<MessageResponse?> GetByIdAsync(Guid messageId);

    Task<ChannelMessagesResponse> GetChannelHistoryAsync(
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize);
}
