using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenBudget.Domain.Entities;

namespace OpenBudget.Infrastructure.Configurations;

public class BotSettingConfiguration : IEntityTypeConfiguration<BotSetting>
{
    public void Configure(EntityTypeBuilder<BotSetting> builder)
    {
        builder.ToTable("BotSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Key)
            .IsUnique();

        builder.Property(x => x.Value)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasMaxLength(250);
    }
}
