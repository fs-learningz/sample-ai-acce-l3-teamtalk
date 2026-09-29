using TeamTalk.Infrastructure.Identity;
using TeamTalk.Infrastructure.Persistence;

namespace TeamTalk.Infrastructure.Tests.Persistence;

public static class TestUserSeeder
{
    public static async Task<ApplicationUser> AddUserAsync(
        TeamTalkDbContext dbContext,
        string displayName,
        string? email = null)
    {
        var id = Guid.CreateVersion7();
        var userEmail = email ?? $"user-{id:N}@example.com";
        var user = new ApplicationUser
        {
            Id = id,
            UserName = userEmail,
            NormalizedUserName = userEmail.ToUpperInvariant(),
            Email = userEmail,
            NormalizedEmail = userEmail.ToUpperInvariant(),
            DisplayName = displayName,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}
