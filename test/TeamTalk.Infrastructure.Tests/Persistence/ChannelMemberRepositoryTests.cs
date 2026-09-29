using TeamTalk.Domain.Entities;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class ChannelMemberRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public ChannelMemberRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ThenIsMemberAsync_ReturnsTrue()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            await new ChannelMemberRepository(writeDbContext)
                .AddAsync(new ChannelMember(Guid.CreateVersion7(), channelId, userId, DateTime.UtcNow));
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var isMember = await new ChannelMemberRepository(readDbContext).IsMemberAsync(channelId, userId);

        // assert
        Assert.True(isMember);
    }

    [Fact]
    public async Task IsMemberAsync_WhenUserIsNotMemberOfChannel_ReturnsFalse()
    {
        // arrange
        var channelId = Guid.NewGuid();
        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            await new ChannelMemberRepository(writeDbContext)
                .AddAsync(new ChannelMember(Guid.CreateVersion7(), channelId, Guid.NewGuid(), DateTime.UtcNow));
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var repository = new ChannelMemberRepository(readDbContext);
        var otherUser = await repository.IsMemberAsync(channelId, Guid.NewGuid());
        var otherChannel = await repository.IsMemberAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.False(otherUser);
        Assert.False(otherChannel);
    }
}
