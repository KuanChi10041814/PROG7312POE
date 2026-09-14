using Microsoft.EntityFrameworkCore;
using PROG7312POE.Core.Models;

namespace PROG7312POE.Api.Data;

public class SmartXDbContext : DbContext
{
    public SmartXDbContext(DbContextOptions<SmartXDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sensor> Sensors => Set<Sensor>();

    public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();

    public DbSet<SensorAttachment> SensorAttachments => Set<SensorAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(sensor => sensor.Id);

            entity.Property(sensor => sensor.DeviceName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(sensor => sensor.MacAddress)
                .IsRequired()
                .HasMaxLength(17);

            entity.HasIndex(sensor => sensor.MacAddress)
                .IsUnique();

            entity.Property(sensor => sensor.DeploymentLocation)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(sensor => sensor.Category)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(sensor => sensor.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasMany(sensor => sensor.TelemetryRecords)
                .WithOne(record => record.Sensor)
                .HasForeignKey(record => record.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(sensor => sensor.Attachments)
                .WithOne(attachment => attachment.Sensor)
                .HasForeignKey(attachment => attachment.SensorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TelemetryRecord>(entity =>
        {
            entity.HasKey(record => record.Id);

            entity.Property(record => record.MetricName)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(record => record.ValueType)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(record => record.RawValue)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(record => record.Severity)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(record => record.RecordedAtUtc);
        });

        modelBuilder.Entity<SensorAttachment>(entity =>
        {
            entity.HasKey(attachment => attachment.Id);

            entity.Property(attachment => attachment.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(attachment => attachment.ContentType)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(attachment => attachment.StoredPath)
                .IsRequired()
                .HasMaxLength(500);
        });
    }
}
