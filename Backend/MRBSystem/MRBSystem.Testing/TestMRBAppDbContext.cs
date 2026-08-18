using Microsoft.EntityFrameworkCore;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;

namespace MRBSystem.Testing;

public class TestMRBAppDbContext : MRBAppDbContext
{
    public TestMRBAppDbContext(DbContextOptions<MRBAppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reservation>()
            .Property(r => r.RowVersion)
            .IsRequired(false)
            .ValueGeneratedNever();
    }
}

