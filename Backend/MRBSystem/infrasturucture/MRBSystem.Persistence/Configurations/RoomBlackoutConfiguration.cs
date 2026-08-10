using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class RoomBlackoutConfiguration : IEntityTypeConfiguration<RoomBlackout>
{
    public void Configure(EntityTypeBuilder<RoomBlackout> builder)
    {
        builder.HasKey(rb => rb.Id);

        builder.Property(rb => rb.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(rb => rb.Room)
            .WithMany()
            .HasForeignKey(rb => rb.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rb => rb.RoomId);
    }
}
