using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Persistence.Data;

public class MRBAppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public MRBAppDbContext(DbContextOptions<MRBAppDbContext> options) : base(options)
    {
    }
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Room_Equipment> RoomEquipment => Set<Room_Equipment>();
    public DbSet<RoomBlackout> RoomBlackouts => Set<RoomBlackout>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Checkin> CheckIns => Set<Checkin>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); 

        builder.ApplyConfigurationsFromAssembly(typeof(MRBAppDbContext).Assembly);
    }
}
