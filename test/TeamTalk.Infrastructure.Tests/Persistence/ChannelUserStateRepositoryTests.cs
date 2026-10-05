using TeamTalk.Domain.Entities;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class ChannelUserStateRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public ChannelUserStateRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ThenGetAsync_ReturnsPersistedState()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var lastRead = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var state = new ChannelUserState(Guid.CreateVersion7(), channelId, userId);
        state.SetPreferences(true, true);
        state.MarkRead(lastRead);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            await new ChannelUserStateRepository(writeDbContext).AddAsync(state);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new ChannelUserStateRepository(readDbContext).GetAsync(channelId, userId);

        // assert
        Assert.NotNull(found);
        Assert.Equal(state.Id, found!.Id);
        Assert.True(found.IsPinned);
        Assert.True(found.IsMuted);
        Assert.Equal(lastRead, found.LastReadAtUtc);
    }

    [Fact]
    public async Task GetAsync_WhenNoStateExists_ReturnsNull()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new ChannelUserStateRepository(dbContext).GetAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task GetAsync_WhenStateBelongsToAnotherUser_ReturnsNull()
    {
        // arrange
        var channelId = Guid.NewGuid();
        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            await new ChannelUserStateRepository(writeDbContext)
                .AddAsync(new ChannelUserState(Guid.CreateVersion7(), channelId, Guid.NewGuid()));
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new ChannelUserStateRepository(readDbContext).GetAsync(channelId, Guid.NewGuid());

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateAsync_WithChangedFlags_PersistsChanges()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            await new ChannelUserStateRepository(seedDbContext)
                .AddAsync(new ChannelUserState(Guid.CreateVersion7(), channelId, userId));
        }

        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            var repository = new ChannelUserStateRepository(updateDbContext);
            var state = await repository.GetAsync(channelId, userId);
            state!.SetPreferences(true, false);

            // act
            await repository.UpdateAsync(state);
        }

        // assert
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new ChannelUserStateRepository(readDbContext).GetAsync(channelId, userId);
        Assert.NotNull(found);
        Assert.True(found!.IsPinned);
        Assert.False(found.IsMuted);
    }
}
