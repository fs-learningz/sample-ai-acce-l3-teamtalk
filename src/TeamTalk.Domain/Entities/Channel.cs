using TeamTalk.Domain.Common;
using TeamTalk.Domain.Enums;

namespace TeamTalk.Domain.Entities;

public class Channel
{
    public const int MaxNameLength = 80;

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public string Name { get; private set; }

    public ChannelType Type { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Channel(Guid id, Guid workspaceId, string name, ChannelType type, Guid createdByUserId, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Channel name is required.");
        }

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Channel name cannot exceed {MaxNameLength} characters.");
        }

        Id = id;
        WorkspaceId = workspaceId;
        Name = name.Trim();
        Type = type;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    private Channel()
    {
        Name = string.Empty;
    }

    public bool IsPublic()
    {
        return Type == ChannelType.Public;
    }
}
