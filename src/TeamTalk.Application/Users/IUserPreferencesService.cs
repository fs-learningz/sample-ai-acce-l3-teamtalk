using ErrorOr;
using TeamTalk.Application.Users.Contracts;

namespace TeamTalk.Application.Users;

public interface IUserPreferencesService
{
    Task<ErrorOr<UserThemeResponse>> GetThemeAsync(Guid userId);

    Task<ErrorOr<UserThemeResponse>> UpdateThemeAsync(Guid userId, UpdateUserThemeRequest request);
}
