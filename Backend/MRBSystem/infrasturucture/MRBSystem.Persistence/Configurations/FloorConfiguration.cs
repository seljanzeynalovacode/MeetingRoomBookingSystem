using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Configurations;

public class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(f => f.Building)
            .WithMany()
            .HasForeignKey(f => f.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.BuildingId);
    }
}
