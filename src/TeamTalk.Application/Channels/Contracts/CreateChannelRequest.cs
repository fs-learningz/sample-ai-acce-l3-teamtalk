using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Channels.Contracts;

public class CreateChannelRequest
{
    public required string Name { get; init; }

    public required ChannelType Type { get; init; }
}
