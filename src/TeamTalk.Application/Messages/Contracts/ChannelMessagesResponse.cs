namespace TeamTalk.Application.Messages.Contracts;

public class ChannelMessagesResponse
{
    public required IReadOnlyList<MessageResponse> Items { get; init; }

    public DateTime? NextCursor { get; init; }
}
