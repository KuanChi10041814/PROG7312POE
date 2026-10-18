using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PROG7312POE.Api.Data;
using PROG7312POE.Api.Services;
using PROG7312POE.Core.DataStructures;
using PROG7312POE.Core.DTOs;
using PROG7312POE.Core.Models;

namespace PROG7312POE.Tests;

public sealed class PartTwoTests
{
    [Fact]
    public void QueueProcessesCriticalBeforeWarningsAndPreservesFifoWithinPriority()
    {
        var queue = new TelemetryWorkQueue<string>();
        queue.Enqueue("normal-1", 2);
        queue.Enqueue("warning", 1);
        queue.Enqueue("normal-2", 2);
        queue.Enqueue("critical-1", 0);
        queue.Enqueue("critical-2", 0);
        var actual = new List<string>();
        while (queue.TryDequeue(out var item)) actual.Add(item);
        Assert.Equal(new[] { "critical-1", "critical-2", "warning", "normal-1", "normal-2" }, actual);
    }

    [Fact]
    public async Task UndoRestoresStateInReverseOrderAndExhaustsHistory()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SmartXDbContext>().UseSqlite(connection).Options;
        await using var db = new SmartXDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var sensor = new Sensor { DeviceName = "Pump A", MacAddress = "AA:01", Category = "Actuator" };
        db.Sensors.Add(sensor);
        await db.SaveChangesAsync();
        var service = new CommandStreamService(db);
        await service.IssueCommandAsync(new() { SensorId = sensor.Id, CommandText = "Start pump" });
        Assert.Equal("On", sensor.ControlState);
        await service.IssueCommandAsync(new() { SensorId = sensor.Id, CommandText = "Stop pump" });
        Assert.Equal("Off", sensor.ControlState);
        await service.UndoLastCommandAsync();
        Assert.Equal("On", sensor.ControlState);
        await service.UndoLastCommandAsync();
        Assert.Equal("Off", sensor.ControlState);
        Assert.Null((await service.UndoLastCommandAsync()).Command);
        Assert.Equal(2, await db.CommandRecords.CountAsync(command => command.IsUndo));
        Assert.Equal(2, await db.CommandRecords.CountAsync(command => command.IsUndone));
    }

    [Fact]
    public async Task DashboardFiltersDeduplicatesAlertsAndClearsRecoveredMetrics()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        await using var db = new SmartXDbContext(
            new DbContextOptionsBuilder<SmartXDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var sensor = new Sensor { DeviceName = "Moisture B", MacAddress = "AA:02", Category = "Environmental" };
        db.Sensors.Add(sensor);
        await db.SaveChangesAsync();
        for (var i = 0; i < 2; i++)
            db.TelemetryRecords.Add(new TelemetryRecord { SensorId = sensor.Id, MetricName = "moisture",
                RawValue = "10", IsAnomaly = true, Severity = "Critical", RecordedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = new CommandStreamService(db);
        var dashboard = await service.GetDashboardAsync("critical");
        Assert.Single(dashboard.Devices);
        Assert.Single(dashboard.ActiveErrorStates);
        Assert.Single(dashboard.SuggestedActions);
        Assert.Empty((await service.GetDashboardAsync("missing")).Devices);
        Assert.Single((await service.GetDashboardAsync("Environmental")).Devices);
        db.TelemetryRecords.Add(new TelemetryRecord { SensorId = sensor.Id, MetricName = "moisture",
            RawValue = "50", Severity = "Normal", RecordedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        dashboard = await service.GetDashboardAsync(null);
        Assert.Empty(dashboard.ActiveErrorStates);
        Assert.Empty(dashboard.SuggestedActions);
    }

    [Fact]
    public async Task CommandsRejectMissingDevicesAndEmptyText()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        await using var db = new SmartXDbContext(
            new DbContextOptionsBuilder<SmartXDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var service = new CommandStreamService(db);
        Assert.Null((await service.IssueCommandAsync(new() { SensorId = 999, CommandText = "Start" })).Command);
        Assert.Null((await service.UndoLastCommandAsync()).Command);
    }
}
