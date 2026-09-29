using TeamTalk.Application.Messages.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Infrastructure.Persistence;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class QueryServiceTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly PostgresContainerFixture _fixture;

    public QueryServiceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task AddMessagesAsync(Guid channelId, Guid senderId, int count)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new MessageRepository(dbContext);

        for (var i = 0; i < count; i++)
        {
            await repository.AddAsync(new Message(Guid.CreateVersion7(), channelId, senderId, $"message {i}", BaseTime.AddMinutes(i)));
        }
    }

    [Fact]
    public async Task MessageQueryService_GetByIdAsync_ReturnsMessageWithSenderDisplayName()
    {
        // arrange
        var channelId = Guid.NewGuid();
        Guid senderId;
        var message = default(Message)!;
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            var sender = await TestUserSeeder.AddUserAsync(seedDbContext, "Alice");
            senderId = sender.Id;
            message = new Message(Guid.CreateVersion7(), channelId, senderId, "hello", BaseTime);
            await new MessageRepository(seedDbContext).AddAsync(message);
        }

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var response = await new MessageQueryService(dbContext).GetByIdAsync(message.Id);

        // assert
        Assert.NotNull(response);
        Assert.Equal("Alice", response!.SenderDisplayName);
        Assert.Equal("hello", response.Content);
        Assert.Equal(senderId, response.SenderId);
        Assert.Equal(channelId, response.ChannelId);
        Assert.False(response.IsDeleted);
    }

    [Fact]
    public async Task MessageQueryService_GetByIdAsync_WhenMessageIsDeleted_ReturnsPlaceholderContent()
    {
        // arrange
        var message = default(Message)!;
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            var sender = await TestUserSeeder.AddUserAsync(seedDbContext, "Alice");
            message = new Message(Guid.CreateVersion7(), Guid.NewGuid(), sender.Id, "secret", BaseTime);
            message.SoftDelete(sender.Id, BaseTime.AddMinutes(1));
            await new MessageRepository(seedDbContext).AddAsync(message);
        }

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var response = await new MessageQueryService(dbContext).GetByIdAsync(message.Id);

        // assert
        Assert.NotNull(response);
        Assert.True(response!.IsDeleted);
        Assert.Equal(MessageResponse.DeletedPlaceholder, response.Content);
    }

    [Fact]
    public async Task MessageQueryService_GetByIdAsync_WhenMessageDoesNotExist_ReturnsNull()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var response = await new MessageQueryService(dbContext).GetByIdAsync(Guid.NewGuid());

        // assert
        Assert.Null(response);
    }

    [Fact]
    public async Task MessageQueryService_GetChannelHistoryAsync_WhenChannelIsEmpty_ReturnsEmptyItemsWithoutCursor()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var history = await new MessageQueryService(dbContext).GetChannelHistoryAsync(Guid.NewGuid(), null, 10);

        // assert
        Assert.Empty(history.Items);
        Assert.Null(history.NextCursor);
    }

    [Fact]
    public async Task MessageQueryService_GetChannelHistoryAsync_WhenMessagesFitInOnePage_ReturnsNoCursor()
    {
        // arrange
        var channelId = Guid.NewGuid();
        Guid senderId;
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            senderId = (await TestUserSeeder.AddUserAsync(seedDbContext, "Alice")).Id;
        }

        await AddMessagesAsync(channelId, senderId, 3);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var history = await new MessageQueryService(dbContext).GetChannelHistoryAsync(channelId, null, 3);

        // assert
        Assert.Equal(3, history.Items.Count);
        Assert.Null(history.NextCursor);
        Assert.Equal(["message 2", "message 1", "message 0"], history.Items.Select(m => m.Content).ToArray());
    }

    [Fact]
    public async Task MessageQueryService_GetChannelHistoryAsync_WhenMoreMessagesThanPageSize_ReturnsCursorOfLastItem()
    {
        // arrange
        var channelId = Guid.NewGuid();
        Guid senderId;
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            senderId = (await TestUserSeeder.AddUserAsync(seedDbContext, "Alice")).Id;
        }

        await AddMessagesAsync(channelId, senderId, 5);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var history = await new MessageQueryService(dbContext).GetChannelHistoryAsync(channelId, null, 2);

        // assert
        Assert.Equal(["message 4", "message 3"], history.Items.Select(m => m.Content).ToArray());
        Assert.Equal(BaseTime.AddMinutes(3), history.NextCursor);
    }

    [Fact]
    public async Task MessageQueryService_GetChannelHistoryAsync_WithBeforeCursor_ReturnsOnlyOlderMessages()
    {
        // arrange
        var channelId = Guid.NewGuid();
        Guid senderId;
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            senderId = (await TestUserSeeder.AddUserAsync(seedDbContext, "Alice")).Id;
        }

        await AddMessagesAsync(channelId, senderId, 5);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var history = await new MessageQueryService(dbContext).GetChannelHistoryAsync(channelId, BaseTime.AddMinutes(3), 10);

        // assert
        Assert.Equal(["message 2", "message 1", "message 0"], history.Items.Select(m => m.Content).ToArray());
        Assert.Null(history.NextCursor);
    }

    [Fact]
    public async Task WorkspaceMemberQueryService_GetForWorkspaceAsync_ReturnsMembersWithUserDetailsOrderedByJoinDate()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        Guid firstUserId;
        Guid secondUserId;
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            var first = await TestUserSeeder.AddUserAsync(seedDbContext, "First User");
            var second = await TestUserSeeder.AddUserAsync(seedDbContext, "Second User");
            firstUserId = first.Id;
            secondUserId = second.Id;

            var memberRepository = new WorkspaceMemberRepository(seedDbContext);
            await memberRepository.AddAsync(new WorkspaceMember(Guid.CreateVersion7(), workspaceId, secondUserId, WorkspaceRole.Member, BaseTime.AddMinutes(5)));
            await memberRepository.AddAsync(new WorkspaceMember(Guid.CreateVersion7(), workspaceId, firstUserId, WorkspaceRole.Admin, BaseTime));
        }

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var members = await new WorkspaceMemberQueryService(dbContext).GetForWorkspaceAsync(workspaceId);

        // assert
        Assert.Equal(2, members.Count);
        Assert.Equal(firstUserId, members[0].UserId);
        Assert.Equal("First User", members[0].DisplayName);
        Assert.Equal(WorkspaceRole.Admin, members[0].Role);
        Assert.False(string.IsNullOrWhiteSpace(members[0].Email));
        Assert.Equal(secondUserId, members[1].UserId);
        Assert.Equal(WorkspaceRole.Member, members[1].Role);
    }

    [Fact]
    public async Task WorkspaceMemberQueryService_GetForWorkspaceAsync_ExcludesMembersOfOtherWorkspaces()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            var user = await TestUserSeeder.AddUserAsync(seedDbContext, "Someone");
            await new WorkspaceMemberRepository(seedDbContext)
                .AddAsync(new WorkspaceMember(Guid.CreateVersion7(), Guid.NewGuid(), user.Id, WorkspaceRole.Member, BaseTime));
        }

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var members = await new WorkspaceMemberQueryService(dbContext).GetForWorkspaceAsync(workspaceId);

        // assert
        Assert.Empty(members);
    }

    [Fact]
    public async Task WorkspaceQueryService_GetSummaryAsync_ReturnsCreatorDetailsAndMemberCount()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            var creator = await TestUserSeeder.AddUserAsync(seedDbContext, "Creator", $"creator-{Guid.NewGuid():N}@example.com");
            var workspace = new Workspace(workspaceId, "Engineering", creator.Id, BaseTime);
            await new WorkspaceRepository(seedDbContext).AddAsync(workspace);
            var memberRepository = new WorkspaceMemberRepository(seedDbContext);
            await memberRepository.AddAsync(new WorkspaceMember(Guid.CreateVersion7(), workspaceId, creator.Id, WorkspaceRole.Admin, BaseTime));
            await memberRepository.AddAsync(new WorkspaceMember(Guid.CreateVersion7(), workspaceId, Guid.NewGuid(), WorkspaceRole.Member, BaseTime));
        }

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var summary = await new WorkspaceQueryService(dbContext).GetSummaryAsync(workspaceId);

        // assert
        Assert.NotNull(summary);
        Assert.Equal("Creator", summary!.CreatedByDisplayName);
        Assert.StartsWith("creator-", summary.CreatedByEmail);
        Assert.Equal(2, summary.MemberCount);
    }

    [Fact]
    public async Task WorkspaceQueryService_GetSummaryAsync_WhenCreatorUserIsMissing_ReturnsEmptyCreatorFields()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            await new WorkspaceRepository(seedDbContext).AddAsync(new Workspace(workspaceId, "Orphaned", Guid.NewGuid(), BaseTime));
        }

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var summary = await new WorkspaceQueryService(dbContext).GetSummaryAsync(workspaceId);

        // assert
        Assert.NotNull(summary);
        Assert.Equal(string.Empty, summary!.CreatedByDisplayName);
        Assert.Equal(string.Empty, summary.CreatedByEmail);
        Assert.Equal(0, summary.MemberCount);
    }

    [Fact]
    public async Task WorkspaceQueryService_GetSummaryAsync_WhenWorkspaceDoesNotExist_ReturnsNull()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var summary = await new WorkspaceQueryService(dbContext).GetSummaryAsync(Guid.NewGuid());

        // assert
        Assert.Null(summary);
    }
}
