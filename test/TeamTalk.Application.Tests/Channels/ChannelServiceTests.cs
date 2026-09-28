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
        _workspaceMemberRepository.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);

        var request = new CreateChannelRequest { Name = "general", Type = ChannelType.Public };
        var result = await _sut.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        Assert.True(result.IsError);
        Assert.Equal(WorkspaceErrors.NotAMember, result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ReturnsConflict()
    {
        _workspaceMemberRepository.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(true);
        _channelRepository.NameExistsInWorkspaceAsync(Arg.Any<Guid>(), "general").Returns(true);

        var request = new CreateChannelRequest { Name = "general", Type = ChannelType.Public };
        var result = await _sut.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NameAlreadyExists, result.FirstError);
    }

    [Fact]
    public async Task EnsureCanPostAsync_ForPublicChannelUserHasNotJoined_AutoJoinsAndSucceeds()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), workspaceId, "general", ChannelType.Public, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _workspaceMemberRepository.IsMemberAsync(workspaceId, userId).Returns(true);
        _channelMemberRepository.IsMemberAsync(channel.Id, userId).Returns(false);

        var result = await _sut.EnsureCanPostAsync(channel.Id, userId);

        Assert.False(result.IsError);
        await _channelMemberRepository.Received(1).AddAsync(
            Arg.Is<ChannelMember>(m => m.ChannelId == channel.Id && m.UserId == userId));
    }

    [Fact]
    public async Task CanUserAccessChannelAsync_ForPrivateChannelNonMember_ReturnsForbidden()
    {
        var channel = new Channel(Guid.NewGuid(), Guid.NewGuid(), "secret", ChannelType.Private, Guid.NewGuid(), DateTime.UtcNow);
        _channelRepository.GetByIdAsync(channel.Id).Returns(channel);
        _channelMemberRepository.IsMemberAsync(channel.Id, Arg.Any<Guid>()).Returns(false);

        var result = await _sut.CanUserAccessChannelAsync(channel.Id, Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
    }
}
