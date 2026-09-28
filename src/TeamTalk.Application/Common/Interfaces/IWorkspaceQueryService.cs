using TeamTalk.Application.Common.Models;

namespace TeamTalk.Application.Common.Interfaces;

public interface IWorkspaceQueryService
{
    Task<WorkspaceSummary?> GetSummaryAsync(Guid workspaceId);
}
