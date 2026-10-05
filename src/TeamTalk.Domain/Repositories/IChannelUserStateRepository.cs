using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IChannelUserStateRepository
{
    Task<ChannelUserState?> GetAsync(Guid channelId, Guid userId);

    Task AddAsync(ChannelUserState state);

    Task UpdateAsync(ChannelUserState state);
}
