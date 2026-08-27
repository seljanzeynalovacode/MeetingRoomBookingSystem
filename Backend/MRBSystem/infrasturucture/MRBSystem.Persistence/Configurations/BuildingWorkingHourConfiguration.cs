using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class BuildingWorkingHourConfiguration : IEntityTypeConfiguration<BuildingWorkingHour>
{
    public void Configure(EntityTypeBuilder<BuildingWorkingHour> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(w => w.Building)
            .WithMany()
            .HasForeignKey(w => w.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.BuildingId, w.DayOfWeek }).IsUnique();
    }
}