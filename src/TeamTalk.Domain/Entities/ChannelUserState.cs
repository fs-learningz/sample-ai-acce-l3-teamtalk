namespace TeamTalk.Domain.Entities;

public class ChannelUserState
{
    public Guid Id { get; private set; }

    public Guid ChannelId { get; private set; }

    public Guid UserId { get; private set; }

    public bool IsPinned { get; private set; }

    public bool IsMuted { get; private set; }

    public DateTime? LastReadAtUtc { get; private set; }

    public ChannelUserState(Guid id, Guid channelId, Guid userId)
    {
        Id = id;
        ChannelId = channelId;
        UserId = userId;
    }

    private ChannelUserState()
    {
    }

    public void SetPreferences(bool isPinned, bool isMuted)
    {
        IsPinned = isPinned;
        IsMuted = isMuted;
    }

    public void MarkRead(DateTime nowUtc)
    {
        LastReadAtUtc = nowUtc;
    }
}
