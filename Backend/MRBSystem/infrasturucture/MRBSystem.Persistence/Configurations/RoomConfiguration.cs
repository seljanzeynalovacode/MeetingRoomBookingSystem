using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(r => r.Floor)
            .WithMany()
            .HasForeignKey(r => r.FloorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.FloorId);
    }
}
