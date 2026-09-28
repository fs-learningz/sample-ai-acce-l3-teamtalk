using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Api.IntegrationTests.Infrastructure;

public class NoOpMessageBroadcastService : IMessageBroadcastService
{
    public Task BroadcastMessageSentAsync(MessageResponse message)
    {
        return Task.CompletedTask;
    }

    public Task BroadcastMessageEditedAsync(MessageResponse message)
    {
        return Task.CompletedTask;
    }

    public Task BroadcastMessageDeletedAsync(MessageResponse message)
    {
        return Task.CompletedTask;
    }
}
