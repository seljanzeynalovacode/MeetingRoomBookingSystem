using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Building)
            .WithMany()
            .HasForeignKey(s => s.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.BuildingId).IsUnique();
    }
}