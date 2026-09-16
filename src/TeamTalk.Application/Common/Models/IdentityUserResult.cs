namespace TeamTalk.Application.Common.Models;

public class IdentityUserResult
{
    public required Guid UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }
}
