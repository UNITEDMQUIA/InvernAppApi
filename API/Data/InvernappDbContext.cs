using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Data;

public class InvernAppDbContext : DbContext
{
    public InvernAppDbContext(DbContextOptions<InvernAppDbContext> options)
        : base(options)
    {

    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<Configuration> Configurations => Set<Configuration>();
    public DbSet<ActuatorState> ActuatorStates => Set<ActuatorState>();
    public DbSet<Command> Commands => Set<Command>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Carga todas las clases IEntityTypeConfiguration<T> de Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvernAppDbContext).Assembly);

        // Todas las fechas se guardan y se leen como UTC.
        var utcConverter = new UtcDateTimeConverter();
        var nullableUtcConverter = new NullableUtcDateTimeConverter();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                }
            }
        }
    }
}