using Microsoft.EntityFrameworkCore;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Databases;

public class TransitDbContext : DbContext
{
    public TransitDbContext(DbContextOptions<TransitDbContext> options) : base(options) { }

    public DbSet<Agency> Agencies { get; set; }
    public DbSet<Calendar> Calendars { get; set; }
    public DbSet<CalendarDate> CalendarDates { get; set; }
    public DbSet<FareAttribute> FareAttributes { get; set; }
    public DbSet<FareRule> FareRules { get; set; }
    public DbSet<Models.Route> Routes { get; set; }
    public DbSet<Shape> Shapes { get; set; }
    public DbSet<Stop> Stops { get; set; }
    public DbSet<StopTime> StopTimes { get; set; }
    public DbSet<Transfer> Transfers { get; set; }
    public DbSet<Trip> Trips { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Chaves prim�rias
        modelBuilder.Entity<Agency>().HasKey(e => e.Id);
        modelBuilder.Entity<Calendar>().HasKey(e => e.Id);
        modelBuilder.Entity<CalendarDate>().HasKey(e => e.Id);
        modelBuilder.Entity<FareAttribute>().HasKey(e => e.Id);
        modelBuilder.Entity<FareRule>().HasKey(e => e.Id);
        modelBuilder.Entity<Models.Route>().HasKey(e => e.Id);
        modelBuilder.Entity<Shape>().HasKey(e => e.Id);
        modelBuilder.Entity<Stop>().HasKey(e => e.Id);
        modelBuilder.Entity<StopTime>().HasKey(e => e.Id);
        modelBuilder.Entity<Transfer>().HasKey(e => e.Id);
        modelBuilder.Entity<Trip>().HasKey(e => e.Id);

        // Indexes principais
        modelBuilder.Entity<Agency>().HasIndex(e => e.AgencyId);
        modelBuilder.Entity<Calendar>().HasIndex(e => e.ServiceId);
        modelBuilder.Entity<CalendarDate>().HasIndex(e => e.ServiceId);
        modelBuilder.Entity<FareAttribute>().HasIndex(e => e.FareId);
        modelBuilder.Entity<FareRule>().HasIndex(e => e.FareId);
        modelBuilder.Entity<Models.Route>().HasIndex(e => e.RouteId);
        modelBuilder.Entity<Shape>().HasIndex(e => e.ShapeId);
        modelBuilder.Entity<Stop>().HasIndex(e => e.StopId);
        modelBuilder.Entity<StopTime>().HasIndex(e => e.TripId);
        modelBuilder.Entity<StopTime>().HasIndex(e => e.StopId);
        modelBuilder.Entity<Transfer>().HasIndex(e => e.FromStopId);
        modelBuilder.Entity<Transfer>().HasIndex(e => e.ToStopId);
        modelBuilder.Entity<Trip>().HasIndex(e => e.TripId);
        modelBuilder.Entity<Trip>().HasIndex(e => e.RouteId);
        modelBuilder.Entity<Trip>().HasIndex(e => e.ServiceId);
    }
}
