using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenBudget.Domain.Entities;

namespace OpenBudget.Infrastructure.Configurations;

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PhoneNumber).IsUnique();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.RejectReason).HasMaxLength(200);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.BrokerId);

        builder.HasOne(x => x.Broker)
            .WithMany(u => u.CollectedVotes)
            .HasForeignKey(x => x.BrokerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ConfirmedByAdmin)
            .WithMany(u => u.ConfirmedVotes)
            .HasForeignKey(x => x.ConfirmedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
