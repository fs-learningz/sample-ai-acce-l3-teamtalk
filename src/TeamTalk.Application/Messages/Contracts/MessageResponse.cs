namespace TeamTalk.Application.Messages.Contracts;

public class MessageResponse
{
    public const string DeletedPlaceholder = "[deleted]";

    public required Guid Id { get; init; }

    public required Guid ChannelId { get; init; }

    public required Guid SenderId { get; init; }

    public required string SenderDisplayName { get; init; }

    public required string Content { get; init; }

    public required DateTime SentAtUtc { get; init; }

    public DateTime? EditedAtUtc { get; init; }

    public required bool IsDeleted { get; init; }
}
