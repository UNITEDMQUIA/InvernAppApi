using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvernApp.Api.Data.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeviceId).IsRequired().HasMaxLength(50);

        // Clave alterna: DeviceId es único y es la clave a la que apuntan las tablas hijas.
        builder.HasAlternateKey(d => d.DeviceId).HasName("UQ_Devices_DeviceId");

        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(d => d.RegisteredAt).IsRequired();
    }
}