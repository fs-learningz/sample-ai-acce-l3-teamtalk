using ErrorOr;
using NSubstitute;
using TeamTalk.Application.Channels;
using TeamTalk.Application.Channels.Contracts;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Tests.Channels;

public class ChannelServiceTests
{
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelMemberRepository _channelMemberRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IChannelUserStateRepository _channelUserStateRepository;
    private readonly IChannelQueryService _channelQueryService;
    private readonly ChannelService _sut;

    public ChannelServiceTests()
    {
        _channelRepository = Substitute.For<IChannelRepository>();
        _channelMemberRepository = Substitute.For<IChannelMemberRepository>();
        _workspaceMemberRepository = Substitute.For<IWorkspaceMemberRepository>();
        _channelUserStateRepository = Substitute.For<IChannelUserStateRepository>();
        _channelQueryService = Substitute.For<IChannelQueryService>();
        _sut = new ChannelService(
            _channelRepository,
            _channelMemberRepository,
            _workspaceMemberRepository,
            _channelUserStateRepository,
            _channelQueryService);
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
        var general = new ChannelResponse
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = "general",
            Type = ChannelType.Public,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        var secret = new ChannelResponse
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = "secret",
            Type = ChannelType.Private,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelQueryService.GetVisibleAsync(workspaceId, userId).Returns(new List<ChannelResponse> { general, secret });

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
        _channelQueryService.GetVisibleAsync(workspaceId, userId).Returns(new List<ChannelResponse>());

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

    [Fact]
    public async Task LeaveAsync_WhenUserIsChannelMember_RemovesMembership()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(true);

        // act
        var result = await _sut.LeaveAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.False(result.IsError);
        await _channelMemberRepository.Received(1).RemoveAsync(channel.Id, userId);
    }

    [Fact]
    public async Task LeaveAsync_WhenUserIsNotChannelMember_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(false);

        // act
        var result = await _sut.LeaveAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotAMember, result.FirstError);
        await _channelMemberRepository.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsWorkspaceAdmin_DeletesChannel()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        var admin = new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns(admin);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var result = await _sut.DeleteAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.False(result.IsError);
        await _channelRepository.Received(1).DeleteAsync(channel.Id);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        var member = new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns(member);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var result = await _sut.DeleteAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAnAdmin, result.FirstError);
        await _channelRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenNoStateExists_CreatesState()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var request = new UpdateChannelPreferencesRequest { IsPinned = true, IsMuted = true };
        var result = await _sut.UpdatePreferencesAsync(userId, workspaceId, channel.Id, request);

        // assert
        Assert.False(result.IsError);
        await _channelUserStateRepository.Received(1).AddAsync(
            Arg.Is<ChannelUserState>(s => s.ChannelId == channel.Id && s.UserId == userId && s.IsPinned && s.IsMuted));
    }

    [Fact]
    public async Task MarkReadAsync_WhenStateExists_UpdatesLastRead()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);
        var state = new ChannelUserState(Guid.NewGuid(), channel.Id, userId);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelUserStateRepository.GetAsync(channel.Id, userId).Returns(state);

        // act
        var result = await _sut.MarkReadAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.False(result.IsError);
        Assert.NotNull(state.LastReadAtUtc);
        await _channelUserStateRepository.Received(1).UpdateAsync(state);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenUserCanAccessChannel_ReturnsDetailsFromQueryService()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);
        var details = new ChannelDetailsResponse
        {
            Id = channel.Id,
            WorkspaceId = workspaceId,
            Name = "general",
            Type = ChannelType.Public,
            CreatedByUserId = userId,
            CreatedByDisplayName = "Alice",
            CreatedAtUtc = channel.CreatedAtUtc,
            MemberCount = 3,
            IsMember = true,
            IsPinned = false,
            IsMuted = false,
        };
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelQueryService.GetDetailsAsync(channel.Id, userId).Returns(details);

        // act
        var result = await _sut.GetDetailsAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Same(details, result.Value);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(false);

        // act
        var result = await _sut.GetDetailsAsync(userId, workspaceId, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
        await _channelQueryService.DidNotReceive().GetDetailsAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetDetailsAsync_WhenChannelDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Channel?)null);

        // act
        var result = await _sut.GetDetailsAsync(userId, workspaceId, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenChannelBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var result = await _sut.GetDetailsAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task GetDetailsAsync_ForPrivateChannelNonMember_ReturnsNotVisible()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(false);

        // act
        var result = await _sut.GetDetailsAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenQueryServiceReturnsNull_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelQueryService.GetDetailsAsync(channel.Id, userId).Returns((ChannelDetailsResponse?)null);

        // act
        var result = await _sut.GetDetailsAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task MarkReadAsync_WhenNoStateExists_AddsStateWithLastRead()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelUserStateRepository.GetAsync(channel.Id, userId).Returns((ChannelUserState?)null);

        // act
        var result = await _sut.MarkReadAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.False(result.IsError);
        await _channelUserStateRepository.Received(1).AddAsync(
            Arg.Is<ChannelUserState>(s => s.ChannelId == channel.Id && s.UserId == userId && s.LastReadAtUtc != null));
        await _channelUserStateRepository.DidNotReceive().UpdateAsync(Arg.Any<ChannelUserState>());
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenStateExists_UpdatesFlagsWithoutAdding()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, userId, DateTime.UtcNow);
        var state = new ChannelUserState(Guid.NewGuid(), channel.Id, userId);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelUserStateRepository.GetAsync(channel.Id, userId).Returns(state);

        // act
        var request = new UpdateChannelPreferencesRequest { IsPinned = true, IsMuted = false };
        var result = await _sut.UpdatePreferencesAsync(userId, workspaceId, channel.Id, request);

        // assert
        Assert.False(result.IsError);
        Assert.True(state.IsPinned);
        Assert.False(state.IsMuted);
        await _channelUserStateRepository.Received(1).UpdateAsync(state);
        await _channelUserStateRepository.DidNotReceive().AddAsync(Arg.Any<ChannelUserState>());
    }

    [Fact]
    public async Task MarkReadAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(false);

        // act
        var result = await _sut.MarkReadAsync(userId, workspaceId, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
        await _channelUserStateRepository.DidNotReceive().AddAsync(Arg.Any<ChannelUserState>());
        await _channelUserStateRepository.DidNotReceive().UpdateAsync(Arg.Any<ChannelUserState>());
    }

    [Fact]
    public async Task MarkReadAsync_WhenChannelBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var result = await _sut.MarkReadAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
        await _channelUserStateRepository.DidNotReceive().AddAsync(Arg.Any<ChannelUserState>());
    }

    [Fact]
    public async Task MarkReadAsync_ForPrivateChannelNonMember_ReturnsNotVisible()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(false);

        // act
        var result = await _sut.MarkReadAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
        await _channelUserStateRepository.DidNotReceive().AddAsync(Arg.Any<ChannelUserState>());
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(false);

        // act
        var request = new UpdateChannelPreferencesRequest { IsPinned = true, IsMuted = true };
        var result = await _sut.UpdatePreferencesAsync(userId, workspaceId, Guid.NewGuid(), request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
        await _channelUserStateRepository.DidNotReceive().AddAsync(Arg.Any<ChannelUserState>());
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenChannelBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var request = new UpdateChannelPreferencesRequest { IsPinned = true, IsMuted = true };
        var result = await _sut.UpdatePreferencesAsync(userId, workspaceId, channel.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
        await _channelUserStateRepository.DidNotReceive().AddAsync(Arg.Any<ChannelUserState>());
    }

    [Fact]
    public async Task UpdatePreferencesAsync_ForPrivateChannelNonMember_ReturnsNotVisible()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(false);

        // act
        var request = new UpdateChannelPreferencesRequest { IsPinned = true, IsMuted = true };
        var result = await _sut.UpdatePreferencesAsync(userId, workspaceId, channel.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
        await _channelUserStateRepository.DidNotReceive().AddAsync(Arg.Any<ChannelUserState>());
    }

    [Fact]
    public async Task LeaveAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(false);

        // act
        var result = await _sut.LeaveAsync(userId, workspaceId, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
        await _channelMemberRepository.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task LeaveAsync_WhenChannelBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var result = await _sut.LeaveAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
        await _channelMemberRepository.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.DeleteAsync(userId, workspaceId, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
        await _channelRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteAsync_WhenChannelDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var admin = new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns(admin);
        _channelRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Channel?)null);

        // act
        var result = await _sut.DeleteAsync(userId, workspaceId, Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
        await _channelRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteAsync_WhenChannelBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        var admin = new WorkspaceMember(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, userId).Returns(admin);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);

        // act
        var result = await _sut.DeleteAsync(userId, workspaceId, channel.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotFound, result.FirstError);
        await _channelRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }
}
