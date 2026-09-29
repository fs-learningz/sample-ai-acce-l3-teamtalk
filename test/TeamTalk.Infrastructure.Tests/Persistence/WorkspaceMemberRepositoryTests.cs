using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class WorkspaceMemberRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public WorkspaceMemberRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task AddMemberAsync(Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        await using var dbContext = _fixture.CreateDbContext();
        await new WorkspaceMemberRepository(dbContext)
            .AddAsync(new WorkspaceMember(Guid.CreateVersion7(), workspaceId, userId, role, DateTime.UtcNow));
    }

    [Fact]
    public async Task AddAsync_ThenGetAsync_ReturnsPersistedMember()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, userId, WorkspaceRole.Admin);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceMemberRepository(dbContext).GetAsync(workspaceId, userId);

        // assert
        Assert.NotNull(found);
        Assert.Equal(WorkspaceRole.Admin, found!.Role);
        Assert.Equal(userId, found.UserId);
    }

    [Fact]
    public async Task GetAsync_WhenUserIsNotMember_ReturnsNull()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceMemberRepository(dbContext).GetAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task IsMemberAsync_ReturnsTrueOnlyForMembersOfThatWorkspace()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, userId, WorkspaceRole.Member);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new WorkspaceMemberRepository(dbContext);
        var isMember = await repository.IsMemberAsync(workspaceId, userId);
        var isMemberOfOtherWorkspace = await repository.IsMemberAsync(Guid.NewGuid(), userId);
        var otherUserIsMember = await repository.IsMemberAsync(workspaceId, Guid.NewGuid());

        // assert
        Assert.True(isMember);
        Assert.False(isMemberOfOtherWorkspace);
        Assert.False(otherUserIsMember);
    }

    [Fact]
    public async Task CountAdminsAsync_CountsOnlyAdminsOfThatWorkspace()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, Guid.NewGuid(), WorkspaceRole.Admin);
        await AddMemberAsync(workspaceId, Guid.NewGuid(), WorkspaceRole.Admin);
        await AddMemberAsync(workspaceId, Guid.NewGuid(), WorkspaceRole.Member);
        await AddMemberAsync(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Admin);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var count = await new WorkspaceMemberRepository(dbContext).CountAdminsAsync(workspaceId);

        // assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RemoveAsync_RemovesMembershipAndChannelMembershipsInThatWorkspaceOnly()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.CreateVersion7(), workspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);
        var otherChannel = new Channel(Guid.CreateVersion7(), otherWorkspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);

        await AddMemberAsync(workspaceId, userId, WorkspaceRole.Member);
        await AddMemberAsync(otherWorkspaceId, userId, WorkspaceRole.Member);
        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            var channelRepository = new ChannelRepository(seedDbContext);
            await channelRepository.AddAsync(channel);
            await channelRepository.AddAsync(otherChannel);
            var channelMemberRepository = new ChannelMemberRepository(seedDbContext);
            await channelMemberRepository.AddAsync(new ChannelMember(Guid.CreateVersion7(), channel.Id, userId, DateTime.UtcNow));
            await channelMemberRepository.AddAsync(new ChannelMember(Guid.CreateVersion7(), otherChannel.Id, userId, DateTime.UtcNow));
        }

        await using (var removeDbContext = _fixture.CreateDbContext())
        {
            // act
            await new WorkspaceMemberRepository(removeDbContext).RemoveAsync(workspaceId, userId);
        }

        // assert
        await using var readDbContext = _fixture.CreateDbContext();
        Assert.False(await new WorkspaceMemberRepository(readDbContext).IsMemberAsync(workspaceId, userId));
        Assert.False(await new ChannelMemberRepository(readDbContext).IsMemberAsync(channel.Id, userId));
        Assert.True(await new WorkspaceMemberRepository(readDbContext).IsMemberAsync(otherWorkspaceId, userId));
        Assert.True(await new ChannelMemberRepository(readDbContext).IsMemberAsync(otherChannel.Id, userId));
    }

    [Fact]
    public async Task TryLeaveAsync_WhenRegularMember_RemovesMemberAndReturnsTrue()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, Guid.NewGuid(), WorkspaceRole.Admin);
        await AddMemberAsync(workspaceId, userId, WorkspaceRole.Member);

        // act
        bool left;
        await using (var leaveDbContext = _fixture.CreateDbContext())
        {
            left = await new WorkspaceMemberRepository(leaveDbContext).TryLeaveAsync(workspaceId, userId);
        }

        // assert
        Assert.True(left);
        await using var readDbContext = _fixture.CreateDbContext();
        Assert.False(await new WorkspaceMemberRepository(readDbContext).IsMemberAsync(workspaceId, userId));
    }

    [Fact]
    public async Task TryLeaveAsync_WhenAdminAndAnotherAdminExists_RemovesMemberAndReturnsTrue()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var leavingAdminId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, leavingAdminId, WorkspaceRole.Admin);
        await AddMemberAsync(workspaceId, Guid.NewGuid(), WorkspaceRole.Admin);

        // act
        bool left;
        await using (var leaveDbContext = _fixture.CreateDbContext())
        {
            left = await new WorkspaceMemberRepository(leaveDbContext).TryLeaveAsync(workspaceId, leavingAdminId);
        }

        // assert
        Assert.True(left);
        await using var readDbContext = _fixture.CreateDbContext();
        Assert.Equal(1, await new WorkspaceMemberRepository(readDbContext).CountAdminsAsync(workspaceId));
    }

    [Fact]
    public async Task TryLeaveAsync_WhenSoleAdmin_ReturnsFalseAndKeepsMembership()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, adminId, WorkspaceRole.Admin);
        await AddMemberAsync(workspaceId, Guid.NewGuid(), WorkspaceRole.Member);

        // act
        bool left;
        await using (var leaveDbContext = _fixture.CreateDbContext())
        {
            left = await new WorkspaceMemberRepository(leaveDbContext).TryLeaveAsync(workspaceId, adminId);
        }

        // assert
        Assert.False(left);
        await using var readDbContext = _fixture.CreateDbContext();
        Assert.True(await new WorkspaceMemberRepository(readDbContext).IsMemberAsync(workspaceId, adminId));
    }

    [Fact]
    public async Task TryLeaveAsync_WhenUserIsNotMember_ReturnsTrue()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var left = await new WorkspaceMemberRepository(dbContext).TryLeaveAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(left);
    }

    [Fact]
    public async Task TrySetRoleAsync_WhenPromotingMember_UpdatesRoleAndReturnsTrue()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, userId, WorkspaceRole.Member);

        // act
        bool changed;
        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            changed = await new WorkspaceMemberRepository(updateDbContext).TrySetRoleAsync(workspaceId, userId, WorkspaceRole.Admin);
        }

        // assert
        Assert.True(changed);
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceMemberRepository(readDbContext).GetAsync(workspaceId, userId);
        Assert.Equal(WorkspaceRole.Admin, found!.Role);
    }

    [Fact]
    public async Task TrySetRoleAsync_WhenDemotingAdminAndAnotherAdminExists_UpdatesRoleAndReturnsTrue()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, userId, WorkspaceRole.Admin);
        await AddMemberAsync(workspaceId, Guid.NewGuid(), WorkspaceRole.Admin);

        // act
        bool changed;
        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            changed = await new WorkspaceMemberRepository(updateDbContext).TrySetRoleAsync(workspaceId, userId, WorkspaceRole.Member);
        }

        // assert
        Assert.True(changed);
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceMemberRepository(readDbContext).GetAsync(workspaceId, userId);
        Assert.Equal(WorkspaceRole.Member, found!.Role);
    }

    [Fact]
    public async Task TrySetRoleAsync_WhenDemotingSoleAdmin_ReturnsFalseAndKeepsRole()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, userId, WorkspaceRole.Admin);

        // act
        bool changed;
        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            changed = await new WorkspaceMemberRepository(updateDbContext).TrySetRoleAsync(workspaceId, userId, WorkspaceRole.Member);
        }

        // assert
        Assert.False(changed);
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceMemberRepository(readDbContext).GetAsync(workspaceId, userId);
        Assert.Equal(WorkspaceRole.Admin, found!.Role);
    }

    [Fact]
    public async Task TrySetRoleAsync_WhenUserIsNotMember_ReturnsTrue()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var changed = await new WorkspaceMemberRepository(dbContext).TrySetRoleAsync(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Admin);

        // assert
        Assert.True(changed);
    }

    [Fact]
    public async Task AddAsync_WhenUserIsAlreadyMemberOfWorkspace_ThrowsDbUpdateException()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await AddMemberAsync(workspaceId, userId, WorkspaceRole.Member);

        // act & assert
        await Assert.ThrowsAsync<DbUpdateException>(() => AddMemberAsync(workspaceId, userId, WorkspaceRole.Admin));
    }
}
