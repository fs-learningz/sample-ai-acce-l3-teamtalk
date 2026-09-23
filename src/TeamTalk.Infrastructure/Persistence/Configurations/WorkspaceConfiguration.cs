using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamTalk.Domain.Entities;

namespace TeamTalk.Infrastructure.Persistence.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.Name)
            .HasMaxLength(Workspace.MaxNameLength)
            .IsRequired();

        builder.Property(w => w.Description)
            .HasMaxLength(Workspace.MaxDescriptionLength);

        builder.Property(w => w.IconContentType)
            .HasMaxLength(100);

        builder.Property(w => w.CreatedByUserId).IsRequired();
        builder.Property(w => w.CreatedAtUtc).IsRequired();
        builder.Property(w => w.UpdatedAtUtc).IsRequired();

        builder.Ignore(w => w.HasIcon);
    }
}
