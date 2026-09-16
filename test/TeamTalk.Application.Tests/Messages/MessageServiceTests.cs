using ErrorOr;
using NSubstitute;
using TeamTalk.Application.Channels;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Messages;
using TeamTalk.Application.Messages.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Tests.Messages;

public class MessageServiceTests
{
    private readonly IMessageRepository _messageRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IChannelService _channelService;
    private readonly IMessageQueryService _messageQueryService;
    private readonly IMessageBroadcastService _messageBroadcastService;
    private readonly MessageService _sut;

    public MessageServiceTests()
    {
        _messageRepository = Substitute.For<IMessageRepository>();
        _workspaceMemberRepository = Substitute.For<IWorkspaceMemberRepository>();
        _channelService = Substitute.For<IChannelService>();
        _messageQueryService = Substitute.For<IMessageQueryService>();
        _messageBroadcastService = Substitute.For<IMessageBroadcastService>();
        _sut = new MessageService(
            _messageRepository,
            _workspaceMemberRepository,
            _channelService,
            _messageQueryService,
            _messageBroadcastService);
    }

    private static MessageResponse BuildResponse(Guid id, Guid channelId, Guid senderId, bool isDeleted = false)
    {
        return new MessageResponse
        {
            Id = id,
            ChannelId = channelId,
            SenderId = senderId,
            SenderDisplayName = "Alice",
            Content = isDeleted ? MessageResponse.DeletedPlaceholder : "hello",
            SentAtUtc = DateTime.UtcNow,
            IsDeleted = isDeleted,
        };
    }

    [Fact]
    public async Task SendAsync_WhenUserCanPost_PersistsAndBroadcasts()
    {
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId, Arg.Any<CancellationToken>()).Returns(Result.Success);

        var messageId = Guid.NewGuid();
        _messageQueryService
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(BuildResponse(messageId, channelId, userId));

        var request = new SendMessageRequest { Content = "hello" };
        var result = await _sut.SendAsync(userId, channelId, request, CancellationToken.None);

        Assert.False(result.IsError);
        await _messageRepository.Received(1).AddAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
        await _messageBroadcastService.Received(1).BroadcastMessageSentAsync(Arg.Any<MessageResponse>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenUserCannotAccessChannel_ReturnsError()
    {
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId, Arg.Any<CancellationToken>()).Returns(ChannelErrors.NotVisible);

        var request = new SendMessageRequest { Content = "hello" };
        var result = await _sut.SendAsync(userId, channelId, request, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditAsync_WhenUserIsNotAuthor_ReturnsForbidden()
    {
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);

        var request = new EditMessageRequest { Content = "updated" };
        var result = await _sut.EditAsync(Guid.NewGuid(), channelId, message.Id, request, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotAuthor, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsAuthor_Succeeds()
    {
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);
        _messageQueryService
            .GetByIdAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(message.Id, channelId, authorId, isDeleted: true));

        var result = await _sut.DeleteAsync(authorId, workspaceId, channelId, message.Id, CancellationToken.None);

        Assert.False(result.IsError);
        await _messageRepository.Received(1).UpdateAsync(Arg.Is<Message>(m => m.IsDeleted), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenAlreadyDeleted_ReturnsValidationError()
    {
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        message.SoftDelete(authorId, DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);

        var result = await _sut.DeleteAsync(authorId, workspaceId, channelId, message.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.AlreadyDeleted, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenNonAuthorIsWorkspaceAdmin_Succeeds()
    {
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);
        var adminMembership = new WorkspaceMember(Guid.NewGuid(), workspaceId, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, adminId, Arg.Any<CancellationToken>()).Returns(adminMembership);
        _messageQueryService
            .GetByIdAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(message.Id, channelId, authorId, isDeleted: true));

        var result = await _sut.DeleteAsync(adminId, workspaceId, channelId, message.Id, CancellationToken.None);

        Assert.False(result.IsError);
    }

    [Fact]
    public async Task DeleteAsync_WhenNonAuthorIsNotAdmin_ReturnsForbidden()
    {
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);
        var memberMembership = new WorkspaceMember(Guid.NewGuid(), workspaceId, otherUserId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, otherUserId, Arg.Any<CancellationToken>()).Returns(memberMembership);

        var result = await _sut.DeleteAsync(otherUserId, workspaceId, channelId, message.Id, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotAuthorOrAdmin, result.FirstError);
    }
}
