using Microsoft.EntityFrameworkCore;
using esp32webapp.Models;

namespace esp32webapp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Rule> Rules => Set<Rule>();
}