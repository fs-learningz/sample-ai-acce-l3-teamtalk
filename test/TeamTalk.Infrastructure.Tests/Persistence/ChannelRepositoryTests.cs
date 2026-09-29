using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class ChannelRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public ChannelRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private static Channel NewChannel(Guid workspaceId, string name, ChannelType type)
    {
        return new Channel(Guid.CreateVersion7(), workspaceId, name, type, Guid.NewGuid(), DateTime.UtcNow);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsPersistedChannel()
    {
        // arrange
        var channel = NewChannel(Guid.NewGuid(), "general", ChannelType.Public);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            await new ChannelRepository(writeDbContext).AddAsync(channel);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new ChannelRepository(readDbContext).GetByIdAsync(channel.Id);

        // assert
        Assert.NotNull(found);
        Assert.Equal("general", found!.Name);
        Assert.Equal(channel.WorkspaceId, found.WorkspaceId);
        Assert.Equal(ChannelType.Public, found.Type);
    }

    [Fact]
    public async Task GetByIdAsync_WhenChannelDoesNotExist_ReturnsNull()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new ChannelRepository(dbContext).GetByIdAsync(Guid.NewGuid());

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task NameExistsInWorkspaceAsync_ReturnsTrueOnlyForSameWorkspaceAndName()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            await new ChannelRepository(writeDbContext).AddAsync(NewChannel(workspaceId, "general", ChannelType.Public));
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var repository = new ChannelRepository(readDbContext);
        var sameWorkspaceSameName = await repository.NameExistsInWorkspaceAsync(workspaceId, "general");
        var sameWorkspaceOtherName = await repository.NameExistsInWorkspaceAsync(workspaceId, "random");
        var otherWorkspaceSameName = await repository.NameExistsInWorkspaceAsync(Guid.NewGuid(), "general");

        // assert
        Assert.True(sameWorkspaceSameName);
        Assert.False(sameWorkspaceOtherName);
        Assert.False(otherWorkspaceSameName);
    }

    [Fact]
    public async Task GetVisibleToUserAsync_ReturnsPublicChannelsAndPrivateChannelsUserBelongsTo()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var publicChannel = NewChannel(workspaceId, "public-one", ChannelType.Public);
        var joinedPrivate = NewChannel(workspaceId, "joined-private", ChannelType.Private);
        var otherPrivate = NewChannel(workspaceId, "other-private", ChannelType.Private);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var channelRepository = new ChannelRepository(writeDbContext);
            await channelRepository.AddAsync(publicChannel);
            await channelRepository.AddAsync(joinedPrivate);
            await channelRepository.AddAsync(otherPrivate);
            await new ChannelMemberRepository(writeDbContext)
                .AddAsync(new ChannelMember(Guid.CreateVersion7(), joinedPrivate.Id, userId, DateTime.UtcNow));
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var visible = await new ChannelRepository(readDbContext).GetVisibleToUserAsync(workspaceId, userId);

        // assert
        Assert.Equal(["joined-private", "public-one"], visible.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task GetVisibleToUserAsync_ExcludesChannelsFromOtherWorkspaces()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();
        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var channelRepository = new ChannelRepository(writeDbContext);
            await channelRepository.AddAsync(NewChannel(workspaceId, "mine", ChannelType.Public));
            await channelRepository.AddAsync(NewChannel(otherWorkspaceId, "theirs", ChannelType.Public));
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var visible = await new ChannelRepository(readDbContext).GetVisibleToUserAsync(workspaceId, Guid.NewGuid());

        // assert
        var single = Assert.Single(visible);
        Assert.Equal("mine", single.Name);
    }

    [Fact]
    public async Task GetVisibleToUserAsync_ReturnsChannelsOrderedByName()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var channelRepository = new ChannelRepository(writeDbContext);
            await channelRepository.AddAsync(NewChannel(workspaceId, "charlie", ChannelType.Public));
            await channelRepository.AddAsync(NewChannel(workspaceId, "alpha", ChannelType.Public));
            await channelRepository.AddAsync(NewChannel(workspaceId, "bravo", ChannelType.Public));
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var visible = await new ChannelRepository(readDbContext).GetVisibleToUserAsync(workspaceId, Guid.NewGuid());

        // assert
        Assert.Equal(["alpha", "bravo", "charlie"], visible.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task GetVisibleToUserAsync_WhenWorkspaceHasNoChannels_ReturnsEmptyList()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var visible = await new ChannelRepository(dbContext).GetVisibleToUserAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.Empty(visible);
    }
}
