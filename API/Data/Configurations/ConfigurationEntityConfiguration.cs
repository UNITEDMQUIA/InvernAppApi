using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvernApp.Api.Data.Configurations;

public class ConfigurationEntityConfiguration : IEntityTypeConfiguration<Configuration>
{
    public void Configure(EntityTypeBuilder<Configuration> builder)
    {
        builder.ToTable("Configurations", t =>
        {
            t.HasCheckConstraint("CK_Configurations_SingleRow", "[Id] = 1");
            t.HasCheckConstraint("CK_Configurations_SoilHumidity",
                "[SoilHumidityMinimum] >= 0 AND [SoilHumidityTarget] <= 100 AND [SoilHumidityMinimum] < [SoilHumidityTarget]");
            t.HasCheckConstraint("CK_Configurations_Temperature",
                "[TemperatureHysteresis] > 0 AND [CriticalTemperature] > [MaximumTemperature]");
            t.HasCheckConstraint("CK_Configurations_Luminosity",
                "[MinimumLuminosity] >= 0 AND [TargetLuminosity] <= 100 AND [MinimumLuminosity] < [TargetLuminosity]");
            t.HasCheckConstraint("CK_Configurations_WateringTime",
                "[MaximumWateringSeconds] > 0");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        // Fila única inicial. Valores de laboratorio; el riego queda deshabilitado
        // hasta que exista (o se simule a propósito) el sensor de humedad del suelo.
        builder.HasData(new Configuration
        {
            Id = 1,
            SoilHumidityMinimum = 30,
            SoilHumidityTarget = 45,
            MaximumWateringSeconds = 30,
            MaximumTemperature = 30,
            TemperatureHysteresis = 2,
            CriticalTemperature = 35,
            MinimumLuminosity = 30,
            TargetLuminosity = 45,
            WateringEnabled = false,
            VentilationEnabled = true,
            LightingEnabled = true,
            UpdatedAt = new DateTime(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}