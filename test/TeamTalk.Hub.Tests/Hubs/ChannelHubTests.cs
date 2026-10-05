using System.Security.Claims;
using ErrorOr;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using TeamTalk.Application.Channels;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Domain.Repositories;
using TeamTalk.Hub.Hubs;

namespace TeamTalk.Hub.Tests.Hubs;

public class ChannelHubTests
{
    private const string ConnectionId = "connection-1";

    private readonly IChannelService _channelService;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly HubCallerContext _context;
    private readonly IGroupManager _groups;
    private readonly ChannelHub _sut;

    public ChannelHubTests()
    {
        _channelService = Substitute.For<IChannelService>();
        _workspaceMemberRepository = Substitute.For<IWorkspaceMemberRepository>();
        _context = Substitute.For<HubCallerContext>();
        _context.ConnectionId.Returns(ConnectionId);
        _groups = Substitute.For<IGroupManager>();
        _sut = new ChannelHub(_channelService, _workspaceMemberRepository)
        {
            Context = _context,
            Groups = _groups,
        };
    }

    private void SetUser(params Claim[] claims)
    {
        _context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
    }

    [Fact]
    public async Task JoinChannelAsync_WhenUserCanAccessChannel_AddsConnectionToChannelGroup()
    {
        // arrange
        var userId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        SetUser(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        _channelService.CanUserAccessChannelAsync(channelId, userId).Returns(Result.Success);

        // act
        await _sut.JoinChannelAsync(channelId);

        // assert
        await _groups.Received(1).AddToGroupAsync(ConnectionId, $"channel-{channelId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinChannelAsync_WhenAccessIsDenied_ThrowsHubExceptionWithErrorDescription()
    {
        // arrange
        var userId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        SetUser(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        _channelService.CanUserAccessChannelAsync(channelId, userId).Returns(ChannelErrors.NotVisible);

        // act
        var exception = await Assert.ThrowsAsync<HubException>(() => _sut.JoinChannelAsync(channelId));

        // assert
        Assert.Equal(ChannelErrors.NotVisible.Description, exception.Message);
        await _groups.DidNotReceiveWithAnyArgs().AddToGroupAsync(default!, default!, default);
    }

    [Fact]
    public async Task JoinChannelAsync_WhenChannelDoesNotExist_ThrowsHubException()
    {
        // arrange
        var userId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        SetUser(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        _channelService.CanUserAccessChannelAsync(channelId, userId).Returns(ChannelErrors.NotFound);

        // act
        var exception = await Assert.ThrowsAsync<HubException>(() => _sut.JoinChannelAsync(channelId));

        // assert
        Assert.Equal(ChannelErrors.NotFound.Description, exception.Message);
    }

    [Fact]
    public async Task JoinChannelAsync_WhenUserIdentityClaimIsMissing_ThrowsHubExceptionWithoutCheckingAccess()
    {
        // arrange
        SetUser(new Claim(ClaimTypes.Email, "alice@example.com"));

        // act
        var exception = await Assert.ThrowsAsync<HubException>(() => _sut.JoinChannelAsync(Guid.NewGuid()));

        // assert
        Assert.Equal("Missing user identity.", exception.Message);
        await _channelService.DidNotReceiveWithAnyArgs().CanUserAccessChannelAsync(default, default);
    }

    [Fact]
    public async Task JoinChannelAsync_WhenThereIsNoUser_ThrowsHubException()
    {
        // arrange
        _context.User.Returns((ClaimsPrincipal?)null);

        // act & assert
        await Assert.ThrowsAsync<HubException>(() => _sut.JoinChannelAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task LeaveChannelAsync_RemovesConnectionFromChannelGroup()
    {
        // arrange
        var channelId = Guid.NewGuid();

        // act
        await _sut.LeaveChannelAsync(channelId);

        // assert
        await _groups.Received(1).RemoveFromGroupAsync(ConnectionId, $"channel-{channelId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GroupName_PrefixesChannelIdWithChannelDash()
    {
        // arrange
        var channelId = Guid.NewGuid();

        // act
        var name = ChannelHub.GroupName(channelId);

        // assert
        Assert.Equal($"channel-{channelId}", name);
    }

    [Fact]
    public void GroupName_ForDifferentChannels_ReturnsDifferentNames()
    {
        // act
        var first = ChannelHub.GroupName(Guid.NewGuid());
        var second = ChannelHub.GroupName(Guid.NewGuid());

        // assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task JoinWorkspaceAsync_WhenUserIsMember_AddsConnectionToWorkspaceGroup()
    {
        // arrange
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SetUser(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);

        // act
        await _sut.JoinWorkspaceAsync(workspaceId);

        // assert
        await _groups.Received(1).AddToGroupAsync(
            ConnectionId,
            ChannelHub.WorkspaceGroupName(workspaceId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinWorkspaceAsync_WhenUserIsNotMember_ThrowsHubException()
    {
        // arrange
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SetUser(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(false);

        // act / assert
        await Assert.ThrowsAsync<HubException>(() => _sut.JoinWorkspaceAsync(workspaceId));
        await _groups.DidNotReceive().AddToGroupAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
