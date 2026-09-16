using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Channels.Contracts;

public class ChannelResponse
{
    public required Guid Id { get; init; }

    public required Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public required ChannelType Type { get; init; }

    public required Guid CreatedByUserId { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
}
