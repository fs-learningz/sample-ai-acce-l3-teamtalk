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
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId).Returns(Result.Success);

        var messageId = Guid.NewGuid();
        _messageQueryService
            .GetByIdAsync(Arg.Any<Guid>())
            .Returns(BuildResponse(messageId, channelId, userId));

        // act
        var request = new SendMessageRequest { Content = "hello" };
        var result = await _sut.SendAsync(userId, channelId, request);

        // assert
        Assert.False(result.IsError);
        await _messageRepository.Received(1).AddAsync(Arg.Any<Message>());
        await _messageBroadcastService.Received(1).BroadcastMessageSentAsync(Arg.Any<MessageResponse>());
    }

    [Fact]
    public async Task SendAsync_WhenUserCannotAccessChannel_ReturnsError()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId).Returns(ChannelErrors.NotVisible);

        // act
        var request = new SendMessageRequest { Content = "hello" };
        var result = await _sut.SendAsync(userId, channelId, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<Message>());
    }

    [Fact]
    public async Task EditAsync_WhenUserIsNotAuthor_ReturnsForbidden()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);

        // act
        var request = new EditMessageRequest { Content = "updated" };
        var result = await _sut.EditAsync(Guid.NewGuid(), channelId, message.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotAuthor, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsAuthor_Succeeds()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);
        _messageQueryService
            .GetByIdAsync(message.Id)
            .Returns(BuildResponse(message.Id, channelId, authorId, isDeleted: true));

        // act
        var result = await _sut.DeleteAsync(authorId, workspaceId, channelId, message.Id);

        // assert
        Assert.False(result.IsError);
        await _messageRepository.Received(1).UpdateAsync(Arg.Is<Message>(m => m.IsDeleted));
    }

    [Fact]
    public async Task DeleteAsync_WhenAlreadyDeleted_ReturnsValidationError()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        message.SoftDelete(authorId, DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);

        // act
        var result = await _sut.DeleteAsync(authorId, workspaceId, channelId, message.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.AlreadyDeleted, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenNonAuthorIsWorkspaceAdmin_Succeeds()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);
        var adminMembership = new WorkspaceMember(Guid.NewGuid(), workspaceId, adminId, WorkspaceRole.Admin, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, adminId).Returns(adminMembership);
        _messageQueryService
            .GetByIdAsync(message.Id)
            .Returns(BuildResponse(message.Id, channelId, authorId, isDeleted: true));

        // act
        var result = await _sut.DeleteAsync(adminId, workspaceId, channelId, message.Id);

        // assert
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task DeleteAsync_WhenNonAuthorIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);
        var memberMembership = new WorkspaceMember(Guid.NewGuid(), workspaceId, otherUserId, WorkspaceRole.Member, DateTime.UtcNow);
        _workspaceMemberRepository.GetAsync(workspaceId, otherUserId).Returns(memberMembership);

        // act
        var result = await _sut.DeleteAsync(otherUserId, workspaceId, channelId, message.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotAuthorOrAdmin, result.FirstError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_WhenContentIsBlank_ReturnsValidationError(string content)
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId).Returns(Result.Success);

        // act
        var request = new SendMessageRequest { Content = content };
        var result = await _sut.SendAsync(userId, channelId, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("Message.InvalidContent", result.FirstError.Code);
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<Message>());
    }

    [Fact]
    public async Task SendAsync_WhenContentExceedsMaxLength_ReturnsValidationError()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId).Returns(Result.Success);

        // act
        var request = new SendMessageRequest { Content = new string('a', Message.MaxContentLength + 1) };
        var result = await _sut.SendAsync(userId, channelId, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Message.InvalidContent", result.FirstError.Code);
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<Message>());
    }

    [Fact]
    public async Task SendAsync_WhenContentIsExactlyMaxLength_Succeeds()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId).Returns(Result.Success);
        _messageQueryService
            .GetByIdAsync(Arg.Any<Guid>())
            .Returns(BuildResponse(Guid.NewGuid(), channelId, userId));

        // act
        var request = new SendMessageRequest { Content = new string('a', Message.MaxContentLength) };
        var result = await _sut.SendAsync(userId, channelId, request);

        // assert
        Assert.False(result.IsError);
        await _messageRepository.Received(1).AddAsync(Arg.Any<Message>());
    }

    [Fact]
    public async Task SendAsync_WhenPersistedMessageCannotBeLoaded_ReturnsNotFoundAndDoesNotBroadcast()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId).Returns(Result.Success);
        _messageQueryService.GetByIdAsync(Arg.Any<Guid>()).Returns((MessageResponse?)null);

        // act
        var request = new SendMessageRequest { Content = "hello" };
        var result = await _sut.SendAsync(userId, channelId, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotFound, result.FirstError);
        await _messageBroadcastService.DidNotReceive().BroadcastMessageSentAsync(Arg.Any<MessageResponse>());
    }

    [Fact]
    public async Task SendAsync_WhenUserCanPost_PersistsMessageWithSenderAndContent()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.EnsureCanPostAsync(channelId, userId).Returns(Result.Success);
        _messageQueryService
            .GetByIdAsync(Arg.Any<Guid>())
            .Returns(BuildResponse(Guid.NewGuid(), channelId, userId));

        // act
        var request = new SendMessageRequest { Content = "hello team" };
        await _sut.SendAsync(userId, channelId, request);

        // assert
        await _messageRepository.Received(1).AddAsync(
            Arg.Is<Message>(m => m.ChannelId == channelId && m.SenderId == userId && m.Content == "hello team"));
    }

    [Fact]
    public async Task EditAsync_WhenMessageDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _messageRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Message?)null);

        // act
        var request = new EditMessageRequest { Content = "updated" };
        var result = await _sut.EditAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task EditAsync_WhenMessageBelongsToAnotherChannel_ReturnsNotFound()
    {
        // arrange
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);

        // act
        var request = new EditMessageRequest { Content = "updated" };
        var result = await _sut.EditAsync(authorId, Guid.NewGuid(), message.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task EditAsync_WhenMessageIsDeleted_ReturnsAlreadyDeleted()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        message.SoftDelete(authorId, DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);

        // act
        var request = new EditMessageRequest { Content = "updated" };
        var result = await _sut.EditAsync(authorId, channelId, message.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.AlreadyDeleted, result.FirstError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EditAsync_WhenContentIsBlank_ReturnsValidationError(string content)
    {
        // arrange
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);

        // act
        var request = new EditMessageRequest { Content = content };
        var result = await _sut.EditAsync(authorId, channelId, message.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Message.InvalidContent", result.FirstError.Code);
        await _messageRepository.DidNotReceive().UpdateAsync(Arg.Any<Message>());
    }

    [Fact]
    public async Task EditAsync_WhenContentExceedsMaxLength_ReturnsValidationError()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);

        // act
        var request = new EditMessageRequest { Content = new string('a', Message.MaxContentLength + 1) };
        var result = await _sut.EditAsync(authorId, channelId, message.Id, request);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Message.InvalidContent", result.FirstError.Code);
    }

    [Fact]
    public async Task EditAsync_WhenAuthorEditsValidContent_UpdatesAndBroadcastsEdit()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);
        var response = BuildResponse(message.Id, channelId, authorId);
        _messageQueryService.GetByIdAsync(message.Id).Returns(response);

        // act
        var request = new EditMessageRequest { Content = "updated" };
        var result = await _sut.EditAsync(authorId, channelId, message.Id, request);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(response, result.Value);
        Assert.Equal("updated", message.Content);
        Assert.NotNull(message.EditedAtUtc);
        await _messageRepository.Received(1).UpdateAsync(message);
        await _messageBroadcastService.Received(1).BroadcastMessageEditedAsync(response);
    }

    [Fact]
    public async Task DeleteAsync_WhenMessageDoesNotExist_ReturnsNotFound()
    {
        // arrange
        _messageRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Message?)null);

        // act
        var result = await _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenMessageBelongsToAnotherChannel_ReturnsNotFound()
    {
        // arrange
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);

        // act
        var result = await _sut.DeleteAsync(authorId, Guid.NewGuid(), Guid.NewGuid(), message.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_WhenNonAuthorIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, Guid.NewGuid(), "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);
        _workspaceMemberRepository.GetAsync(workspaceId, Arg.Any<Guid>()).Returns((WorkspaceMember?)null);

        // act
        var result = await _sut.DeleteAsync(Guid.NewGuid(), workspaceId, channelId, message.Id);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(MessageErrors.NotAuthorOrAdmin, result.FirstError);
        await _messageRepository.DidNotReceive().UpdateAsync(Arg.Any<Message>());
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorDeletes_BroadcastsDeletedMessage()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new Message(Guid.NewGuid(), channelId, authorId, "original", DateTime.UtcNow);
        _messageRepository.GetByIdAsync(message.Id).Returns(message);
        var response = BuildResponse(message.Id, channelId, authorId, isDeleted: true);
        _messageQueryService.GetByIdAsync(message.Id).Returns(response);

        // act
        var result = await _sut.DeleteAsync(authorId, Guid.NewGuid(), channelId, message.Id);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(authorId, message.DeletedByUserId);
        await _messageBroadcastService.Received(1).BroadcastMessageDeletedAsync(response);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenUserCannotAccessChannel_ReturnsError()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.CanUserAccessChannelAsync(channelId, userId).Returns(ChannelErrors.NotVisible);

        // act
        var result = await _sut.GetHistoryAsync(userId, channelId, null, 10);

        // assert
        Assert.True(result.IsError);
        Assert.Equal(ChannelErrors.NotVisible, result.FirstError);
        await _messageQueryService.DidNotReceiveWithAnyArgs().GetChannelHistoryAsync(default, default, default);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(-5, 50)]
    [InlineData(1, 1)]
    [InlineData(25, 25)]
    [InlineData(200, 200)]
    [InlineData(201, 200)]
    [InlineData(500, 200)]
    public async Task GetHistoryAsync_NormalizesPageSize(int requestedPageSize, int expectedPageSize)
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _channelService.CanUserAccessChannelAsync(channelId, userId).Returns(Result.Success);
        _messageQueryService
            .GetChannelHistoryAsync(channelId, null, expectedPageSize)
            .Returns(new ChannelMessagesResponse { Items = [] });

        // act
        var result = await _sut.GetHistoryAsync(userId, channelId, null, requestedPageSize);

        // assert
        Assert.False(result.IsError);
        await _messageQueryService.Received(1).GetChannelHistoryAsync(channelId, null, expectedPageSize);
    }

    [Fact]
    public async Task GetHistoryAsync_PassesCursorToQueryService()
    {
        // arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cursor = DateTime.UtcNow;
        var expected = new ChannelMessagesResponse { Items = [], NextCursor = cursor.AddMinutes(-1) };
        _channelService.CanUserAccessChannelAsync(channelId, userId).Returns(Result.Success);
        _messageQueryService.GetChannelHistoryAsync(channelId, cursor, 10).Returns(expected);

        // act
        var result = await _sut.GetHistoryAsync(userId, channelId, cursor, 10);

        // assert
        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value);
    }
}
