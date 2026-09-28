using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Application.Common.Interfaces;

public interface IMessageBroadcastService
{
    Task BroadcastMessageSentAsync(MessageResponse message);

    Task BroadcastMessageEditedAsync(MessageResponse message);

    Task BroadcastMessageDeletedAsync(MessageResponse message);
}
