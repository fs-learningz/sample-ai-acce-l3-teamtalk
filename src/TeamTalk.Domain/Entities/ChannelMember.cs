namespace TeamTalk.Domain.Entities;

public class ChannelMember
{
    public Guid Id { get; private set; }

    public Guid ChannelId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    public ChannelMember(Guid id, Guid channelId, Guid userId, DateTime joinedAtUtc)
    {
        Id = id;
        ChannelId = channelId;
        UserId = userId;
        JoinedAtUtc = joinedAtUtc;
    }

    private ChannelMember()
    {
    }
}
