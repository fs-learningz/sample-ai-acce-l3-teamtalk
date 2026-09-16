using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Application.Common.Interfaces;

public interface IMessageBroadcastService
{
    Task BroadcastMessageSentAsync(MessageResponse message, CancellationToken cancellationToken);

    Task BroadcastMessageEditedAsync(MessageResponse message, CancellationToken cancellationToken);

    Task BroadcastMessageDeletedAsync(MessageResponse message, CancellationToken cancellationToken);
}
