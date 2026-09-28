using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IChannelMemberRepository
{
    Task<bool> IsMemberAsync(Guid channelId, Guid userId);

    Task AddAsync(ChannelMember member);
}
