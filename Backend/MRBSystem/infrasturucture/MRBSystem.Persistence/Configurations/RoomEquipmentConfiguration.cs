using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class RoomEquipmentConfiguration : IEntityTypeConfiguration<RoomEquipment>
{
    public void Configure(EntityTypeBuilder<RoomEquipment> builder)
    {
        builder.HasKey(re => re.Id);

        builder.Property(re => re.Quantity)
            .IsRequired();

        builder.HasOne(re => re.Room)
            .WithMany()
            .HasForeignKey(re => re.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(re => re.Equipment)
            .WithMany()
            .HasForeignKey(re => re.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Eyni otaqda eyni avadanlıq üçün dublikat sətir yaranmasın
        builder.HasIndex(re => new { re.RoomId, re.EquipmentId })
            .IsUnique();
    }

}
