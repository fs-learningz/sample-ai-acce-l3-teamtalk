namespace TeamTalk.Application.Workspaces.Contracts;

public class UpdateWorkspaceRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}
