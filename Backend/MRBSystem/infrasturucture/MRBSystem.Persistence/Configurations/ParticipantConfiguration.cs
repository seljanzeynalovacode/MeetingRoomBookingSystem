using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.GuestEmail)
            .HasMaxLength(256);

        builder.Property(p => p.GuestName)
            .HasMaxLength(200);

        builder.HasOne(p => p.Reservation)
            .WithMany()
            .HasForeignKey(p => p.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.ReservationId);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ResponseStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.GuestEmail)
            .HasMaxLength(256);
    }
}
