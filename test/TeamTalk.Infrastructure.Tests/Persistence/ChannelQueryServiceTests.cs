using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Infrastructure.Persistence;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class ChannelQueryServiceTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly PostgresContainerFixture _fixture;

    public ChannelQueryServiceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Channel> AddChannelAsync(Guid workspaceId, string name, ChannelType type, Guid? createdBy = null)
    {
        var channel = new Channel(Guid.CreateVersion7(), workspaceId, name, type, createdBy ?? Guid.NewGuid(), BaseTime);
        await using var dbContext = _fixture.CreateDbContext();
        await new ChannelRepository(dbContext).AddAsync(channel);

        return channel;
    }

    private async Task AddChannelMemberAsync(Guid channelId, Guid userId)
    {
        await using var dbContext = _fixture.CreateDbContext();
        await new ChannelMemberRepository(dbContext)
            .AddAsync(new ChannelMember(Guid.CreateVersion7(), channelId, userId, BaseTime));
    }

    private async Task<Message> AddMessageAsync(Guid channelId, Guid senderId, DateTime sentAtUtc)
    {
        var message = new Message(Guid.CreateVersion7(), channelId, senderId, "hello", sentAtUtc);
        await using var dbContext = _fixture.CreateDbContext();
        await new MessageRepository(dbContext).AddAsync(message);

        return message;
    }

    private async Task AddStateAsync(Guid channelId, Guid userId, bool isPinned, bool isMuted, DateTime? lastReadAtUtc)
    {
        var state = new ChannelUserState(Guid.CreateVersion7(), channelId, userId);
        state.SetPreferences(isPinned, isMuted);

        if (lastReadAtUtc is not null)
        {
            state.MarkRead(lastReadAtUtc.Value);
        }

        await using var dbContext = _fixture.CreateDbContext();
        await new ChannelUserStateRepository(dbContext).AddAsync(state);
    }

    [Fact]
    public async Task GetVisibleAsync_ReturnsPublicChannelsAndOwnPrivateChannelsOnly()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var publicChannel = await AddChannelAsync(workspaceId, "public", ChannelType.Public);
        var ownPrivate = await AddChannelAsync(workspaceId, "own-private", ChannelType.Private);
        var otherPrivate = await AddChannelAsync(workspaceId, "other-private", ChannelType.Private);
        var otherWorkspaceChannel = await AddChannelAsync(Guid.NewGuid(), "elsewhere", ChannelType.Public);
        await AddChannelMemberAsync(ownPrivate.Id, userId);
        await AddChannelMemberAsync(otherPrivate.Id, Guid.NewGuid());

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new ChannelQueryService(dbContext).GetVisibleAsync(workspaceId, userId);

        // assert
        var ids = result.Select(c => c.Id).ToList();
        Assert.Contains(publicChannel.Id, ids);
        Assert.Contains(ownPrivate.Id, ids);
        Assert.DoesNotContain(otherPrivate.Id, ids);
        Assert.DoesNotContain(otherWorkspaceChannel.Id, ids);
    }

    [Fact]
    public async Task GetVisibleAsync_OrdersPinnedChannelsFirstThenByName()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await AddChannelAsync(workspaceId, "alpha", ChannelType.Public);
        await AddChannelAsync(workspaceId, "beta", ChannelType.Public);
        var zulu = await AddChannelAsync(workspaceId, "zulu", ChannelType.Public);
        await AddStateAsync(zulu.Id, userId, isPinned: true, isMuted: false, lastReadAtUtc: null);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new ChannelQueryService(dbContext).GetVisibleAsync(workspaceId, userId);

        // assert
        Assert.Equal(new[] { "zulu", "alpha", "beta" }, result.Select(c => c.Name).ToArray());
        Assert.True(result[0].IsPinned);
    }

    [Fact]
    public async Task GetVisibleAsync_WhenUserHasMutedChannel_ReturnsMutedFlagOnlyForThatUser()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var channel = await AddChannelAsync(workspaceId, "general", ChannelType.Public);
        await AddStateAsync(channel.Id, userId, isPinned: false, isMuted: true, lastReadAtUtc: null);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var service = new ChannelQueryService(dbContext);
        var forUser = await service.GetVisibleAsync(workspaceId, userId);
        var forOther = await service.GetVisibleAsync(workspaceId, otherUserId);

        // assert
        Assert.True(forUser.Single().IsMuted);
        Assert.False(forOther.Single().IsMuted);
    }

    [Fact]
    public async Task GetVisibleAsync_WhenOthersSentMessageAndChannelNeverRead_HasUnreadIsTrue()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = await AddChannelAsync(workspaceId, "general", ChannelType.Public);
        await AddMessageAsync(channel.Id, Guid.NewGuid(), BaseTime);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new ChannelQueryService(dbContext).GetVisibleAsync(workspaceId, userId);

        // assert
        Assert.True(result.Single().HasUnread);
    }

    [Fact]
    public async Task GetVisibleAsync_WhenMessageIsNewerThanLastRead_HasUnreadIsTrue()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = await AddChannelAsync(workspaceId, "general", ChannelType.Public);
        await AddStateAsync(channel.Id, userId, false, false, BaseTime);
        await AddMessageAsync(channel.Id, Guid.NewGuid(), BaseTime.AddMinutes(5));

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new ChannelQueryService(dbContext).GetVisibleAsync(workspaceId, userId);

        // assert
        Assert.True(result.Single().HasUnread);
    }

    [Fact]
    public async Task GetVisibleAsync_WhenMessageIsOlderThanLastRead_HasUnreadIsFalse()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = await AddChannelAsync(workspaceId, "general", ChannelType.Public);
        await AddMessageAsync(channel.Id, Guid.NewGuid(), BaseTime);
        await AddStateAsync(channel.Id, userId, false, false, BaseTime.AddMinutes(5));

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new ChannelQueryService(dbContext).GetVisibleAsync(workspaceId, userId);

        // assert
        Assert.False(result.Single().HasUnread);
    }

    [Fact]
    public async Task GetVisibleAsync_WhenOnlyOwnMessagesExist_HasUnreadIsFalse()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = await AddChannelAsync(workspaceId, "general", ChannelType.Public);
        await AddMessageAsync(channel.Id, userId, BaseTime);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new ChannelQueryService(dbContext).GetVisibleAsync(workspaceId, userId);

        // assert
        Assert.False(result.Single().HasUnread);
    }

    [Fact]
    public async Task GetVisibleAsync_WhenOnlyDeletedMessagesFromOthersExist_HasUnreadIsFalse()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = await AddChannelAsync(workspaceId, "general", ChannelType.Public);
        var senderId = Guid.NewGuid();
        var message = new Message(Guid.CreateVersion7(), channel.Id, senderId, "gone", BaseTime);
        message.SoftDelete(senderId, BaseTime.AddMinutes(1));
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            await new MessageRepository(seedDbContext).AddAsync(message);
        }

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new ChannelQueryService(dbContext).GetVisibleAsync(workspaceId, userId);

        // assert
        Assert.False(result.Single().HasUnread);
    }

    [Fact]
    public async Task GetDetailsAsync_ReturnsCreatorNameMemberCountAndUserFlags()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Guid creatorId;
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            creatorId = (await TestUserSeeder.AddUserAsync(seedDbContext, "Alice")).Id;
        }

        var channel = await AddChannelAsync(workspaceId, "general", ChannelType.Public, creatorId);
        await AddChannelMemberAsync(channel.Id, creatorId);
        await AddChannelMemberAsync(channel.Id, userId);
        await AddStateAsync(channel.Id, userId, isPinned: true, isMuted: true, lastReadAtUtc: null);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var details = await new ChannelQueryService(dbContext).GetDetailsAsync(channel.Id, userId);

        // assert
        Assert.NotNull(details);
        Assert.Equal("Alice", details!.CreatedByDisplayName);
        Assert.Equal(creatorId, details.CreatedByUserId);
        Assert.Equal(2, details.MemberCount);
        Assert.True(details.IsMember);
        Assert.True(details.IsPinned);
        Assert.True(details.IsMuted);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenUserIsNotMember_ReturnsIsMemberFalseAndDefaultFlags()
    {
        // arrange
        var channel = await AddChannelAsync(Guid.NewGuid(), "general", ChannelType.Public);
        await AddChannelMemberAsync(channel.Id, Guid.NewGuid());

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var details = await new ChannelQueryService(dbContext).GetDetailsAsync(channel.Id, Guid.NewGuid());

        // assert
        Assert.NotNull(details);
        Assert.False(details!.IsMember);
        Assert.False(details.IsPinned);
        Assert.False(details.IsMuted);
        Assert.Equal(1, details.MemberCount);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenCreatorUserIsMissing_ReturnsUnknownUserName()
    {
        // arrange
        var channel = await AddChannelAsync(Guid.NewGuid(), "general", ChannelType.Public);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var details = await new ChannelQueryService(dbContext).GetDetailsAsync(channel.Id, Guid.NewGuid());

        // assert
        Assert.NotNull(details);
        Assert.Equal("Unknown user", details!.CreatedByDisplayName);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenChannelDoesNotExist_ReturnsNull()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var details = await new ChannelQueryService(dbContext).GetDetailsAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.Null(details);
    }
}
