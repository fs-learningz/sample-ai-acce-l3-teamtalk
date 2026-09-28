using ErrorOr;
using Microsoft.AspNetCore.Identity;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Common.Models;

namespace TeamTalk.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ErrorOr<IdentityUserResult>> RegisterAsync(
        string email,
        string displayName,
        string password)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return AuthErrors.EmailAlreadyExists;
        }

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (createResult.Succeeded is false)
        {
            var description = string.Join(" ", createResult.Errors.Select(e => e.Description));

            return AuthErrors.RegistrationFailed(description);
        }

        return ToResult(user);
    }

    public async Task<ErrorOr<IdentityUserResult>> ValidateCredentialsAsync(
        string email,
        string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);

        if (passwordValid is false)
        {
            return AuthErrors.InvalidCredentials;
        }

        return ToResult(user);
    }

    public async Task<Guid?> FindUserIdByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        return user?.Id;
    }

    private static IdentityUserResult ToResult(ApplicationUser user)
    {
        return new IdentityUserResult
        {
            UserId = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
        };
    }
}
