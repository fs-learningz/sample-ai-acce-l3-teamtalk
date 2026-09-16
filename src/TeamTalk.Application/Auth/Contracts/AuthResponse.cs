namespace TeamTalk.Application.Auth.Contracts;

public class AuthResponse
{
    public required Guid UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public required string Token { get; init; }
}
