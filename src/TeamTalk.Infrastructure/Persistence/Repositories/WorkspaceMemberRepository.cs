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

    public async Task<WorkspaceMember?> GetAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, cancellationToken);
    }

    public async Task<int> CountAdminsAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin, cancellationToken);
    }

    public async Task AddAsync(WorkspaceMember member, CancellationToken cancellationToken)
    {
        await _dbContext.WorkspaceMembers.AddAsync(member, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            await RemoveMembershipRowsAsync(workspaceId, userId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task<bool> TryLeaveAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var member = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, cancellationToken);

            if (member is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return true;
            }

            if (member.Role == WorkspaceRole.Admin)
            {
                var adminCount = await _dbContext.WorkspaceMembers
                    .CountAsync(m => m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin, cancellationToken);

                if (adminCount <= 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            await RemoveMembershipRowsAsync(workspaceId, userId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task<bool> TrySetRoleAsync(
        Guid workspaceId,
        Guid userId,
        WorkspaceRole role,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var member = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, cancellationToken);

            if (member is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return true;
            }

            if (member.Role == WorkspaceRole.Admin && role == WorkspaceRole.Member)
            {
                var adminCount = await _dbContext.WorkspaceMembers
                    .CountAsync(m => m.WorkspaceId == workspaceId && m.Role == WorkspaceRole.Admin, cancellationToken);

                if (adminCount <= 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            member.ChangeRole(role);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    private async Task RemoveMembershipRowsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var channelIds = _dbContext.Channels.Where(c => c.WorkspaceId == workspaceId).Select(c => c.Id);

        await _dbContext.ChannelMembers
            .Where(cm => cm.UserId == userId && channelIds.Contains(cm.ChannelId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
