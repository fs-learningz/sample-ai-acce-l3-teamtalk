using Microsoft.AspNetCore.Identity;

using TeamTalk.Application.Users;

namespace TeamTalk.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public string Theme { get; set; } = UserThemes.Default;
}
