using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenBudget.Domain.Entities;

namespace OpenBudget.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TelegramId).IsUnique();
        
        builder.Property(x => x.Username).HasMaxLength(100);
        builder.Property(x => x.FullName).HasMaxLength(100);
    }
}
