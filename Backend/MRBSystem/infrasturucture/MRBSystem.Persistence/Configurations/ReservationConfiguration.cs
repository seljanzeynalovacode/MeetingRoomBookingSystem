using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.QrCodeToken)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(r => r.QrCodeToken)
            .IsUnique();

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        builder.HasOne(r => r.ReservedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReservedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Room)
            .WithMany()
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CheckIn)
            .WithOne(c => c.Reservation)
            .HasForeignKey<Checkin>(c => c.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Otaqda müəyyən tarix aralığında rezervasiya axtarışını sürətləndirmək üçün
        builder.HasIndex(r => new { r.RoomId, r.StartDate, r.EndDate });
    }
}
