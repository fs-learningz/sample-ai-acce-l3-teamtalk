using ErrorOr;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Users.Contracts;

namespace TeamTalk.Application.Users;

public class UserPreferencesService : IUserPreferencesService
{
    private readonly IIdentityService _identityService;

    public UserPreferencesService(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<ErrorOr<UserThemeResponse>> GetThemeAsync(Guid userId)
    {
        var theme = await _identityService.GetThemeAsync(userId);

        if (theme is null)
        {
            return UserErrors.NotFound;
        }

        return new UserThemeResponse { Theme = theme };
    }

    public async Task<ErrorOr<UserThemeResponse>> UpdateThemeAsync(Guid userId, UpdateUserThemeRequest request)
    {
        if (UserThemes.IsValid(request.Theme) is false)
        {
            return UserErrors.InvalidTheme;
        }

        var updated = await _identityService.SetThemeAsync(userId, request.Theme);

        if (updated is false)
        {
            return UserErrors.NotFound;
        }

        return new UserThemeResponse { Theme = request.Theme };
    }
}
