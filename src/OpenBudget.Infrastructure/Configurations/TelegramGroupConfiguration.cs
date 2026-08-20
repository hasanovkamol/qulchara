using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenBudget.Domain.Entities;

namespace OpenBudget.Infrastructure.Configurations;

public class TelegramGroupConfiguration : IEntityTypeConfiguration<TelegramGroup>
{
    public void Configure(EntityTypeBuilder<TelegramGroup> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ChatId).IsUnique();

        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(100);
    }
}
