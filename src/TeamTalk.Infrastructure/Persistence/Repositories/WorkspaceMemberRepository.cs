using System.Data;
using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Infrastructure.Persistence.Repositories;

public class WorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly TeamTalkDbContext _dbContext;

    public WorkspaceMemberRepository(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkspaceMember?> GetAsync(Guid workspaceId, Guid userId)
    {
        return await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
    }

    public async Task<bool> IsMemberAsync(Guid workspaceId, Guid userId)
    {
        return await _dbContext.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
    }

    public async Task<int> CountAdminsAsync(Guid workspaceId)
    {
        return await _dbContext.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin);
    }

    public async Task AddAsync(WorkspaceMember member)
    {
        await _dbContext.WorkspaceMembers.AddAsync(member);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid workspaceId, Guid userId)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            await RemoveMembershipRowsAsync(workspaceId, userId);

            await transaction.CommitAsync();
        });
    }

    public async Task<bool> TryLeaveAsync(Guid workspaceId, Guid userId)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var member = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

            if (member is null)
            {
                await transaction.CommitAsync();

                return true;
            }

            if (member.Role is WorkspaceRole.Admin)
            {
                var adminCount = await _dbContext.WorkspaceMembers
                    .CountAsync(m => m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin);

                if (adminCount <= 1)
                {
                    await transaction.RollbackAsync();

                    return false;
                }
            }

            await RemoveMembershipRowsAsync(workspaceId, userId);

            await transaction.CommitAsync();

            return true;
        });
    }

    public async Task<bool> TrySetRoleAsync(
        Guid workspaceId,
        Guid userId,
        WorkspaceRole role)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var member = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

            if (member is null)
            {
                await transaction.CommitAsync();

                return true;
            }

            if (member.Role is WorkspaceRole.Admin && role is WorkspaceRole.Member)
            {
                var adminCount = await _dbContext.WorkspaceMembers
                    .CountAsync(m => m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin);

                if (adminCount <= 1)
                {
                    await transaction.RollbackAsync();

                    return false;
                }
            }

            member.ChangeRole(role);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return true;
        });
    }

    private async Task RemoveMembershipRowsAsync(Guid workspaceId, Guid userId)
    {
        var channelIds = _dbContext.Channels.Where(c => c.WorkspaceId == workspaceId).Select(c => c.Id);

        await _dbContext.ChannelMembers
            .Where(cm => cm.UserId == userId && channelIds.Contains(cm.ChannelId))
            .ExecuteDeleteAsync();

        await _dbContext.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
