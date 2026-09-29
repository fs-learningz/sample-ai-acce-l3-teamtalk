using ErrorOr;
using NSubstitute;
using TeamTalk.Application.Channels;
using TeamTalk.Application.Channels.Contracts;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Tests.Channels;

public class ChannelServiceTests
{
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelMemberRepository _channelMemberRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly ChannelService _sut;

    public ChannelServiceTests()
    {
        _channelRepository = Substitute.For<IChannelRepository>();
        _channelMemberRepository = Substitute.For<IChannelMemberRepository>();
        _workspaceMemberRepository = Substitute.For<IWorkspaceMemberRepository>();
        _sut = new ChannelService(_channelRepository, _channelMemberRepository, _workspaceMemberRepository);
    }

    [Fact]
    public async Task CreateAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        _workspaceMemberRepository.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);

        // act
        var request = new CreateChannelRequest { Name = "general", Type = ChannelType.Public };
        var result = await _sut.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ReturnsConflict()
    {
        // arrange
        _workspaceMemberRepository.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(true);
        _channelRepository.NameExistsInWorkspaceAsync(Arg.Any<Guid>(), "general").Returns(true);

        // act
        var request = new CreateChannelRequest { Name = "general", Type = ChannelType.Public };
        var result = await _sut.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NameAlreadyExists, result.FirstError);
    }

    [Fact]
    public async Task EnsureCanPostAsync_ForPublicChannelUserHasNotJoined_AutoJoinsAndSucceeds()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(false);

        // act
        var result = await _sut.EnsureCanPostAsync(channel.Id, userId);

        // assert
        Assert.False(result.IsError);
        await _channelMemberRepository.Received(1).AddAsync(
            Arg.Is<ChannelMember>(m => m.ChannelId == channel.Id && m.UserId == userId));
    }

    [Fact]
    public async Task CanUserAccessChannelAsync_ForPrivateChannelNonMember_ReturnsForbidden()
    {
        // arrange
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, Arg.Any<Guid>()).Returns(false);

        // act
        var result = await _sut.CanUserAccessChannelAsync(channel.Id, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WhenNameIsBlank_ReturnsValidationError(string name)
    {
        // arrange
        _workspaceMemberRepository.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(true);

        // act
        var request = new CreateChannelRequest { Name = name, Type = ChannelType.Public };
        var result = await _sut.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("Channel.NameRequired", result.FirstError.Code);
        await _channelRepository.DidNotReceive().AddAsync(Arg.Any<Channel>());
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsChannelAndCreatorAsChannelMember()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.NameExistsInWorkspaceAsync(workspaceId, "general").Returns(false);

        // act
        var request = new CreateChannelRequest { Name = "general", Type = ChannelType.Private };
        var result = await _sut.CreateAsync(userId, workspaceId, request);

        // assert
        Assert.False(result.IsError);
        Assert.Equal("general", result.Value.Name);
        Assert.Equal(ChannelType.Private, result.Value.Type);
        Assert.Equal(workspaceId, result.Value.WorkspaceId);
        Assert.Equal(userId, result.Value.CreatedByUserId);
        await _channelRepository.Received(1).AddAsync(
            Arg.Is<Channel>(c => c.Id == result.Value.Id && c.WorkspaceId == workspaceId));
        await _channelMemberRepository.Received(1).AddAsync(
            Arg.Is<ChannelMember>(m => m.ChannelId == result.Value.Id && m.UserId == userId));
    }

    [Fact]
    public async Task GetVisibleAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        _workspaceMemberRepository.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);

        // act
        var result = await _sut.GetVisibleAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task GetVisibleAsync_WhenUserIsMember_ReturnsMappedChannels()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var general = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);
        var secret = new Channel(Guid.NewGuid(), workspaceId, "secret", ChannelType.Private, userId, DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetVisibleToUserAsync(workspaceId, userId).Returns(new List<Channel> { general, secret });

        // act
        var result = await _sut.GetVisibleAsync(userId, workspaceId);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(general.Id, result.Value[0].Id);
        Assert.Equal("secret", result.Value[1].Name);
        Assert.Equal(ChannelType.Private, result.Value[1].Type);
    }

    [Fact]
    public async Task GetVisibleAsync_WhenNoChannelsAreVisible_ReturnsEmptyList()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetVisibleToUserAsync(workspaceId, userId).Returns(new List<Channel>());

        // act
        var result = await _sut.GetVisibleAsync(userId, workspaceId);

        // assert
        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task JoinAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        _workspaceMemberRepository.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);

        // act
        var result = await _sut.JoinAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task JoinAsync_WhenChannelDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _workspaceMemberRepository.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(true);
        _channelRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Channel?)null);

        // act
        var result = await _sut.JoinAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task JoinAsync_WhenChannelBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var result = await _sut.JoinAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task JoinAsync_WhenChannelIsPrivate_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var result = await _sut.JoinAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.CannotJoinPrivateChannel, result.FirstError);
        await _channelMemberRepository.DidNotReceive().AddAsync(Arg.Any<ChannelMember>());
    }

    [Fact]
    public async Task JoinAsync_WhenPublicAndNotYetJoined_AddsChannelMember()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(false);

        // act
        var result = await _sut.JoinAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.False(result.IsError);
        await _channelMemberRepository.Received(1).AddAsync(
            Arg.Is<ChannelMember>(m => m.ChannelId == channel.Id && m.UserId == userId));
    }

    [Fact]
    public async Task JoinAsync_WhenPublicAndAlreadyJoined_DoesNotAddDuplicateMember()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(true);

        // act
        var result = await _sut.JoinAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.False(result.IsError);
        await _channelMemberRepository.DidNotReceive().AddAsync(Arg.Any<ChannelMember>());
    }

    [Fact]
    public async Task CanUserAccessChannelAsync_WhenChannelDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _channelRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Channel?)null);

        // act
        var result = await _sut.CanUserAccessChannelAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task CanUserAccessChannelAsync_ForPublicChannelNonWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _workspaceMemberRepository.IsMemberAsync(channel.WorkspaceId, Arg.Any<Guid>()).Returns(false);

        // act
        var result = await _sut.CanUserAccessChannelAsync(channel.Id, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task CanUserAccessChannelAsync_ForPublicChannelWorkspaceMember_Succeeds()
    {
        // arrange
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _workspaceMemberRepository.IsMemberAsync(channel.WorkspaceId, userId).Returns(true);

        // act
        var result = await _sut.CanUserAccessChannelAsync(channel.Id, userId);

        // assert
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task CanUserAccessChannelAsync_ForPrivateChannelMember_Succeeds()
    {
        // arrange
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(true);

        // act
        var result = await _sut.CanUserAccessChannelAsync(channel.Id, userId);

        // assert
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task EnsureCanPostAsync_WhenChannelDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _channelRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Channel?)null);

        // act
        var result = await _sut.EnsureCanPostAsync(Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task EnsureCanPostAsync_ForPublicChannelNonWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _workspaceMemberRepository.IsMemberAsync(channel.WorkspaceId, Arg.Any<Guid>()).Returns(false);

        // act
        var result = await _sut.EnsureCanPostAsync(channel.Id, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
        await _channelMemberRepository.DidNotReceive().AddAsync(Arg.Any<ChannelMember>());
    }

    [Fact]
    public async Task EnsureCanPostAsync_ForPublicChannelAlreadyJoined_DoesNotAddMemberAgain()
    {
        // arrange
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _workspaceMemberRepository.IsMemberAsync(channel.WorkspaceId, userId).Returns(true);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(true);

        // act
        var result = await _sut.EnsureCanPostAsync(channel.Id, userId);

        // assert
        Assert.False(result.IsError);
        await _channelMemberRepository.DidNotReceive().AddAsync(Arg.Any<ChannelMember>());
    }

    [Fact]
    public async Task EnsureCanPostAsync_ForPrivateChannelNonMember_ReturnsForbidden()
    {
        // arrange
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, Arg.Any<Guid>()).Returns(false);

        // act
        var result = await _sut.EnsureCanPostAsync(channel.Id, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
    }

    [Fact]
    public async Task EnsureCanPostAsync_ForPrivateChannelMember_SucceedsWithoutAddingMember()
    {
        // arrange
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(true);

        // act
        var result = await _sut.EnsureCanPostAsync(channel.Id, userId);

        // assert
        Assert.False(result.IsError);
        await _channelMemberRepository.DidNotReceive().AddAsync(Arg.Any<ChannelMember>());
    }
}
