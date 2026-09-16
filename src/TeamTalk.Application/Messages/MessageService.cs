using ErrorOr;
using TeamTalk.Application.Channels;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Messages.Contracts;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Application.Messages;

public class MessageService : IMessageService
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    private readonly IMessageRepository _messageRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IChannelService _channelService;
    private readonly IMessageQueryService _messageQueryService;
    private readonly IMessageBroadcastService _messageBroadcastService;

    public MessageService(
        IMessageRepository messageRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IChannelService channelService,
        IMessageQueryService messageQueryService,
        IMessageBroadcastService messageBroadcastService)
    {
        _messageRepository = messageRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _channelService = channelService;
        _messageQueryService = messageQueryService;
        _messageBroadcastService = messageBroadcastService;
    }

    public async Task<ErrorOr<MessageResponse>> SendAsync(
        Guid userId,
        Guid channelId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await _channelService.EnsureCanPostAsync(channelId, userId, cancellationToken);
        if (accessResult.IsError)
        {
            return accessResult.Errors;
        }

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > Message.MaxContentLength)
        {
            return Error.Validation("Message.InvalidContent", $"Message content must be between 1 and {Message.MaxContentLength} characters.");
        }

        var message = new Message(Guid.CreateVersion7(), channelId, userId, request.Content, DateTime.UtcNow);
        await _messageRepository.AddAsync(message, cancellationToken);

        var response = await _messageQueryService.GetByIdAsync(message.Id, cancellationToken);
        if (response is null)
        {
            return MessageErrors.NotFound;
        }

        await _messageBroadcastService.BroadcastMessageSentAsync(response, cancellationToken);
        return response;
    }

    public async Task<ErrorOr<MessageResponse>> EditAsync(
        Guid userId,
        Guid channelId,
        Guid messageId,
        EditMessageRequest request,
        CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        if (message is null || message.ChannelId != channelId)
        {
            return MessageErrors.NotFound;
        }

        if (message.SenderId != userId)
        {
            return MessageErrors.NotAuthor;
        }

        if (message.IsDeleted)
        {
            return MessageErrors.AlreadyDeleted;
        }

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > Message.MaxContentLength)
        {
            return Error.Validation("Message.InvalidContent", $"Message content must be between 1 and {Message.MaxContentLength} characters.");
        }

        message.Edit(request.Content, DateTime.UtcNow);
        await _messageRepository.UpdateAsync(message, cancellationToken);

        var response = await _messageQueryService.GetByIdAsync(message.Id, cancellationToken);
        if (response is null)
        {
            return MessageErrors.NotFound;
        }

        await _messageBroadcastService.BroadcastMessageEditedAsync(response, cancellationToken);
        return response;
    }

    public async Task<ErrorOr<Success>> DeleteAsync(
        Guid userId,
        Guid workspaceId,
        Guid channelId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        if (message is null || message.ChannelId != channelId)
        {
            return MessageErrors.NotFound;
        }

        if (message.IsDeleted)
        {
            return MessageErrors.AlreadyDeleted;
        }

        var isAuthor = message.SenderId == userId;
        if (!isAuthor)
        {
            var membership = await _workspaceMemberRepository.GetAsync(workspaceId, userId, cancellationToken);
            var isAdmin = membership is not null && membership.IsAdmin();
            if (!isAdmin)
            {
                return MessageErrors.NotAuthorOrAdmin;
            }
        }

        message.SoftDelete(userId, DateTime.UtcNow);
        await _messageRepository.UpdateAsync(message, cancellationToken);

        var response = await _messageQueryService.GetByIdAsync(message.Id, cancellationToken);
        if (response is null)
        {
            return MessageErrors.NotFound;
        }

        await _messageBroadcastService.BroadcastMessageDeletedAsync(response, cancellationToken);
        return Result.Success;
    }

    public async Task<ErrorOr<ChannelMessagesResponse>> GetHistoryAsync(
        Guid userId,
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var accessResult = await _channelService.CanUserAccessChannelAsync(channelId, userId, cancellationToken);
        if (accessResult.IsError)
        {
            return accessResult.Errors;
        }

        var effectivePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return await _messageQueryService.GetChannelHistoryAsync(channelId, beforeUtc, effectivePageSize, cancellationToken);
    }
}
