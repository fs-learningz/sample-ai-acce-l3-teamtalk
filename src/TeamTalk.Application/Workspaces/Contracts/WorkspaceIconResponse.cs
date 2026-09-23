namespace TeamTalk.Application.Workspaces.Contracts;

public class WorkspaceIconResponse
{
    public required byte[] Data { get; init; }

    public required string ContentType { get; init; }
}
