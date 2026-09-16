namespace TeamTalk.Application.Auth.Contracts;

public class RegisterRequest
{
    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public required string Password { get; init; }
}
