using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamTalk.Domain.Entities;

namespace TeamTalk.Infrastructure.Persistence.Configurations;

public class ChannelUserStateConfiguration : IEntityTypeConfiguration<ChannelUserState>
{
    public void Configure(EntityTypeBuilder<ChannelUserState> builder)
    {
        builder.ToTable("ChannelUserStates");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.ChannelId).IsRequired();
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.IsPinned).IsRequired();
        builder.Property(s => s.IsMuted).IsRequired();

        builder.HasIndex(s => new { s.ChannelId, s.UserId }).IsUnique();
    }
}
