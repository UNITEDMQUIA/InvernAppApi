using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvernApp.Api.Data.Configurations;

public class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
{
    public void Configure(EntityTypeBuilder<Measurement> builder)
    {
        builder.ToTable("Measurements", t =>
        {
            // Rangos físicos razonables (red de seguridad; la API valida antes con mensajes claros).
            t.HasCheckConstraint("CK_Measurements_Temperature",
                "[Temperature] >= -40 AND [Temperature] <= 80");
            t.HasCheckConstraint("CK_Measurements_AirHumidity",
                "[AirHumidity] >= 0 AND [AirHumidity] <= 100");
            t.HasCheckConstraint("CK_Measurements_SoilHumidity",
                "[SoilHumidity] IS NULL OR ([SoilHumidity] >= 0 AND [SoilHumidity] <= 100)");
            t.HasCheckConstraint("CK_Measurements_Luminosity",
                "[Luminosity] >= 0 AND [Luminosity] <= 100");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.DeviceId).IsRequired().HasMaxLength(50);
        builder.Property(m => m.SequenceNumber).IsRequired();
        builder.Property(m => m.Timestamp).IsRequired();
        builder.Property(m => m.ReceivedAt).IsRequired();

        // Idempotencia: la misma medición no puede guardarse dos veces.
        builder.HasIndex(m => new { m.DeviceId, m.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("UX_Measurements_DeviceId_SequenceNumber");

        // Consultas de historial por dispositivo y fecha.
        builder.HasIndex(m => new { m.DeviceId, m.Timestamp })
            .HasDatabaseName("IX_Measurements_DeviceId_Timestamp");

        builder.HasOne(m => m.Device)
            .WithMany(d => d.Measurements)
            .HasForeignKey(m => m.DeviceId)
            .HasPrincipalKey(d => d.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}