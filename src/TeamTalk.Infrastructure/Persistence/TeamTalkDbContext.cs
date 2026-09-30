using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamTalk.Application.Users;
using TeamTalk.Domain.Entities;
using TeamTalk.Infrastructure.Identity;

namespace TeamTalk.Infrastructure.Persistence;

public class TeamTalkDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public TeamTalkDbContext(DbContextOptions<TeamTalkDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces { get; set; } = null!;

    public DbSet<WorkspaceMember> WorkspaceMembers { get; set; } = null!;

    public DbSet<WorkspaceInvitation> WorkspaceInvitations { get; set; } = null!;

    public DbSet<Channel> Channels { get; set; } = null!;

    public DbSet<ChannelMember> ChannelMembers { get; set; } = null!;

    public DbSet<Message> Messages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(TeamTalkDbContext).Assembly);

        builder.Entity<ApplicationUser>()
            .Property(u => u.Theme)
            .HasMaxLength(32)
            .HasDefaultValue(UserThemes.Default);
    }
}
