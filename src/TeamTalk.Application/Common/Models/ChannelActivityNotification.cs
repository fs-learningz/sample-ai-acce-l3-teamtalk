namespace TeamTalk.Application.Common.Models;

/// <summary>
/// Lightweight notice sent to every connected workspace member when a message is posted,
/// so clients can flag the channel as unread without joining its message group.
/// </summary>
public class ChannelActivityNotification
{
    public required Guid ChannelId { get; init; }

    public required Guid SenderId { get; init; }
}
