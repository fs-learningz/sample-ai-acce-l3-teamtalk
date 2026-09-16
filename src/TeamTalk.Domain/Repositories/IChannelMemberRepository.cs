using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IChannelMemberRepository
{
    Task<bool> IsMemberAsync(Guid channelId, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(ChannelMember member, CancellationToken cancellationToken);
}
