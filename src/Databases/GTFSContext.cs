using Microsoft.EntityFrameworkCore;
using Tranzor.Models;
using Tranzor.Models.External;

namespace Tranzor.Databases;

public class GTFSContext : DbContext
{
    public GTFSContext(DbContextOptions<GTFSContext> options) : base(options) { }

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
    public DbSet<FeedInfo> FeedInfos { get; set; }
    public DbSet<AgencyTranslation> Translations { get; set; }
    public DbSet<Attribution> Attributions { get; set; }
    public DbSet<StopArea> StopAreas { get; set; }
    public DbSet<FareMedia> FareMedias { get; set; }
    public DbSet<FareLegRule> FareLegRules { get; set; }
    public DbSet<FareProduct> FareProducts { get; set; }
    public DbSet<Network> Networks { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<StopCity> StopCities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tables
        modelBuilder.Entity<Agency>().ToTable("gtfs_agencies");
        modelBuilder.Entity<Calendar>().ToTable("gtfs_calendars");
        modelBuilder.Entity<CalendarDate>().ToTable("gtfs_calendar_dates");
        modelBuilder.Entity<FareAttribute>().ToTable("gtfs_fare_attributes");
        modelBuilder.Entity<FareRule>().ToTable("gtfs_fare_rules");
        modelBuilder.Entity<Models.Route>().ToTable("gtfs_routes");
        modelBuilder.Entity<Shape>().ToTable("gtfs_shapes");
        modelBuilder.Entity<Stop>().ToTable("gtfs_stops");
        modelBuilder.Entity<StopTime>().ToTable("gtfs_stop_times");
        modelBuilder.Entity<Transfer>().ToTable("gtfs_transfers");
        modelBuilder.Entity<Trip>().ToTable("gtfs_trips");
        modelBuilder.Entity<FeedInfo>().ToTable("gtfs_feed_info");
        modelBuilder.Entity<AgencyTranslation>().ToTable("gtfs_translations");
        modelBuilder.Entity<Attribution>().ToTable("gtfs_attributions");
        modelBuilder.Entity<StopArea>().ToTable("gtfs_stop_areas");
        modelBuilder.Entity<FareMedia>().ToTable("gtfs_fare_media");
        modelBuilder.Entity<FareLegRule>().ToTable("gtfs_fare_leg_rules");
        modelBuilder.Entity<FareProduct>().ToTable("gtfs_fare_products");
        modelBuilder.Entity<Network>().ToTable("gtfs_networks");
        modelBuilder.Entity<City>().ToTable("cities");
        modelBuilder.Entity<StopCity>().ToTable("stop_cities");

        // Keys
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
        modelBuilder.Entity<FeedInfo>().HasKey(e => e.Id);
        modelBuilder.Entity<AgencyTranslation>().HasKey(e => e.Id);
        modelBuilder.Entity<Attribution>().HasKey(e => e.Id);
        modelBuilder.Entity<StopArea>().HasKey(e => e.Id);
        modelBuilder.Entity<FareMedia>().HasKey(e => e.Id);
        modelBuilder.Entity<FareLegRule>().HasKey(e => e.Id);
        modelBuilder.Entity<FareProduct>().HasKey(e => e.Id);
        modelBuilder.Entity<Network>().HasKey(e => e.Id);
        modelBuilder.Entity<City>().HasKey(e => e.Id);
        modelBuilder.Entity<StopCity>().HasKey(e => e.Id);

        // Indexes
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
        modelBuilder.Entity<FeedInfo>().HasIndex(e => e.FeedPublisherName);
        modelBuilder.Entity<AgencyTranslation>().HasIndex(e => e.TableName);
        modelBuilder.Entity<Attribution>().HasIndex(e => e.OrganizationName);
        modelBuilder.Entity<StopArea>().HasIndex(e => e.StopAreaId);
        modelBuilder.Entity<FareMedia>().HasIndex(e => e.FareMediaId);
        modelBuilder.Entity<FareLegRule>().HasIndex(e => e.FareLegRuleId);
        modelBuilder.Entity<FareProduct>().HasIndex(e => e.FareProductId);
        modelBuilder.Entity<Network>().HasIndex(e => e.NetworkId);

        modelBuilder.Entity<Stop>()
            .Property(s => s.Location)
            .HasColumnType("geography (point)")
            .HasSrid(4326);

        modelBuilder.Entity<Stop>()
            .HasIndex(s => s.Location)
            .HasMethod("GIST");

        modelBuilder.Entity<Shape>()
            .Property(s => s.Geom)
            .HasColumnType("geometry (point)")
            .HasSrid(4326);

        modelBuilder.Entity<Shape>()
            .HasIndex(s => s.Geom)
            .HasMethod("GIST");

        modelBuilder.Entity<City>()
            .Property(c => c.Geom)
            .HasColumnType("geometry(MultiPolygon,4326)")
            .HasSrid(4326);

        modelBuilder.Entity<City>()
            .HasIndex(c => c.Geom)
            .HasMethod("GIST");
    }
}