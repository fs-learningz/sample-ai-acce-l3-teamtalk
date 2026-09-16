using TeamTalk.Application.Messages.Contracts;

namespace TeamTalk.Application.Common.Models;

public class BroadcastMessageRequest
{
    public required string EventName { get; init; }

    public required MessageResponse Message { get; init; }
}
