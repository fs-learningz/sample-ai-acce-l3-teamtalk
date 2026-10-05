using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Channels.Contracts;

public class ChannelDetailsResponse
{
    public required Guid Id { get; init; }

    public required Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public required ChannelType Type { get; init; }

    public required Guid CreatedByUserId { get; init; }

    public required string CreatedByDisplayName { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required int MemberCount { get; init; }

    public required bool IsMember { get; init; }

    public required bool IsPinned { get; init; }

    public required bool IsMuted { get; init; }
}
