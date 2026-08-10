using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class CheckinConfiguration : IEntityTypeConfiguration<Checkin>
{
    public void Configure(EntityTypeBuilder<Checkin> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CheckinTime)
            .IsRequired();

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reservation tərəfi HasOne/WithOne olaraq ReservationConfiguration-da təyin olunub,
        // burada yalnız unique index üçün FK-ni işarələyirik
        builder.HasIndex(c => c.ReservationId)
            .IsUnique();
    }
}
