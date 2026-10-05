namespace TeamTalk.Application.Channels.Contracts;

public class UpdateChannelPreferencesRequest
{
    public required bool IsPinned { get; init; }

    public required bool IsMuted { get; init; }
}
