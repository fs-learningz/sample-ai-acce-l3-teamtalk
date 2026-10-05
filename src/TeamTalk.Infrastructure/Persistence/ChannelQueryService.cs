using Microsoft.EntityFrameworkCore;
using TeamTalk.Application.Channels.Contracts;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Domain.Enums;

namespace TeamTalk.Infrastructure.Persistence;

public class ChannelQueryService : IChannelQueryService
{
    private readonly TeamTalkDbContext _dbContext;

    public ChannelQueryService(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ChannelResponse>> GetVisibleAsync(Guid workspaceId, Guid userId)
    {
        var memberChannelIds = _dbContext.ChannelMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ChannelId);

        var rows = await (
            from c in _dbContext.Channels
            where c.WorkspaceId == workspaceId
                && (c.Type == ChannelType.Public || memberChannelIds.Contains(c.Id))
            join s in _dbContext.ChannelUserStates.Where(s => s.UserId == userId)
                on c.Id equals s.ChannelId into states
            from s in states.DefaultIfEmpty()
            select new
            {
                Channel = c,
                IsPinned = s != null && s.IsPinned,
                IsMuted = s != null && s.IsMuted,
                HasUnread = _dbContext.Messages.Any(m =>
                    m.ChannelId == c.Id
                    && m.SenderId != userId
                    && m.DeletedAtUtc == null
                    && (s == null || s.LastReadAtUtc == null || m.SentAtUtc > s.LastReadAtUtc)),
            })
            .ToListAsync();

        return rows
            .OrderByDescending(r => r.IsPinned)
            .ThenBy(r => r.Channel.Name, StringComparer.Ordinal)
            .Select(r => new ChannelResponse
            {
                Id = r.Channel.Id,
                WorkspaceId = r.Channel.WorkspaceId,
                Name = r.Channel.Name,
                Type = r.Channel.Type,
                CreatedByUserId = r.Channel.CreatedByUserId,
                CreatedAtUtc = r.Channel.CreatedAtUtc,
                IsPinned = r.IsPinned,
                IsMuted = r.IsMuted,
                HasUnread = r.HasUnread,
            })
            .ToList();
    }

    public async Task<ChannelDetailsResponse?> GetDetailsAsync(Guid channelId, Guid userId)
    {
        var row = await (
            from c in _dbContext.Channels
            where c.Id == channelId
            join u in _dbContext.Users on c.CreatedByUserId equals u.Id into creators
            from u in creators.DefaultIfEmpty()
            join s in _dbContext.ChannelUserStates.Where(s => s.UserId == userId)
                on c.Id equals s.ChannelId into states
            from s in states.DefaultIfEmpty()
            select new
            {
                Channel = c,
                CreatedByDisplayName = u != null ? u.DisplayName : null,
                MemberCount = _dbContext.ChannelMembers.Count(m => m.ChannelId == c.Id),
                IsMember = _dbContext.ChannelMembers.Any(m => m.ChannelId == c.Id && m.UserId == userId),
                IsPinned = s != null && s.IsPinned,
                IsMuted = s != null && s.IsMuted,
            })
            .FirstOrDefaultAsync();

        if (row is null)
        {
            return null;
        }

        return new ChannelDetailsResponse
        {
            Id = row.Channel.Id,
            WorkspaceId = row.Channel.WorkspaceId,
            Name = row.Channel.Name,
            Type = row.Channel.Type,
            CreatedByUserId = row.Channel.CreatedByUserId,
            CreatedByDisplayName = row.CreatedByDisplayName ?? "Unknown user",
            CreatedAtUtc = row.Channel.CreatedAtUtc,
            MemberCount = row.MemberCount,
            IsMember = row.IsMember,
            IsPinned = row.IsPinned,
            IsMuted = row.IsMuted,
        };
    }
}
