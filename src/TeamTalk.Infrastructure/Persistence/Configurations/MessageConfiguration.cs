using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamTalk.Domain.Entities;

namespace TeamTalk.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.ChannelId).IsRequired();
        builder.Property(m => m.SenderId).IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(Message.MaxContentLength)
            .IsRequired();

        builder.Property(m => m.SentAtUtc).IsRequired();

        builder.HasIndex(m => new { m.ChannelId, m.SentAtUtc });
    }
}
