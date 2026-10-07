using Microsoft.EntityFrameworkCore;
using esp32webapp.Models;

namespace esp32webapp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<RfidCard> RfidCards => Set<RfidCard>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RfidCard>()
            .HasIndex(c => c.Uid)
            .IsUnique();

        modelBuilder.Entity<RfidCard>().HasData(
            new RfidCard { Id = 1, Uid = "A1B2C3D4", OwnerName = "Adam", IsActive = true },
            new RfidCard { Id = 2, Uid = "11223344", OwnerName = "Host", IsActive = true },
            new RfidCard { Id = 3, Uid = "DEADBEEF", OwnerName = "Stara karta", IsActive = false }
        );
    }
}