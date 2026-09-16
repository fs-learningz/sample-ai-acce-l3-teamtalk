using Microsoft.EntityFrameworkCore;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Messages.Contracts;
using TeamTalk.Domain.Entities;

namespace TeamTalk.Infrastructure.Persistence;

public class MessageQueryService : IMessageQueryService
{
    private readonly TeamTalkDbContext _dbContext;

    public MessageQueryService(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MessageResponse?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var result = await (
            from m in _dbContext.Messages
            join u in _dbContext.Users on m.SenderId equals u.Id
            where m.Id == messageId
            select new { Message = m, SenderDisplayName = u.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        return ToResponse(result.Message, result.SenderDisplayName);
    }

    public async Task<ChannelMessagesResponse> GetChannelHistoryAsync(
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query =
            from m in _dbContext.Messages
            join u in _dbContext.Users on m.SenderId equals u.Id
            where m.ChannelId == channelId
            select new { Message = m, SenderDisplayName = u.DisplayName };

        if (beforeUtc.HasValue)
        {
            query = query.Where(x => x.Message.SentAtUtc < beforeUtc.Value);
        }

        var page = await query
            .OrderByDescending(x => x.Message.SentAtUtc)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).Select(x => ToResponse(x.Message, x.SenderDisplayName)).ToList();
        var nextCursor = hasMore ? items[^1].SentAtUtc : (DateTime?)null;

        return new ChannelMessagesResponse
        {
            Items = items,
            NextCursor = nextCursor,
        };
    }

    private static MessageResponse ToResponse(Message message, string senderDisplayName)
    {
        var isDeleted = message.DeletedAtUtc.HasValue;

        return new MessageResponse
        {
            Id = message.Id,
            ChannelId = message.ChannelId,
            SenderId = message.SenderId,
            SenderDisplayName = senderDisplayName,
            Content = isDeleted ? MessageResponse.DeletedPlaceholder : message.Content,
            SentAtUtc = message.SentAtUtc,
            EditedAtUtc = message.EditedAtUtc,
            IsDeleted = isDeleted,
        };
    }
}
