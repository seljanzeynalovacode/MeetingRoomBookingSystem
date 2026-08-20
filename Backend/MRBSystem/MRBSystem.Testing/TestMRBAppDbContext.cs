using Microsoft.EntityFrameworkCore;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;

namespace MRBSystem.Testing;

public class TestMRBAppDbContext : MRBAppDbContext
{
    public TestMRBAppDbContext(DbContextOptions<MRBAppDbContext> options) : base(options)
    {
    }

}

