using System.Globalization;
using PROG7312POE.Core.DataStructures;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Api.Services;

public sealed class TelemetryProcessor(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private sealed record Work(TelemetryCreateRequest Request,
        TaskCompletionSource<(TelemetryResponse? Telemetry, string? Error)> Completion);
    private readonly TelemetryWorkQueue<Work> _pending = new();
    private readonly SemaphoreSlim _available = new(0);

    public Task<(TelemetryResponse? Telemetry, string? Error)> SubmitAsync(TelemetryCreateRequest request)
    {
        var completion = new TaskCompletionSource<(TelemetryResponse?, string?)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _pending.Enqueue(new Work(request, completion), Priority(request));
        _available.Release();
        return completion.Task;
    }

    private static int Priority(TelemetryCreateRequest request)
    {
        var metric = request.MetricName.ToLowerInvariant();
        if (double.TryParse(request.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && ((metric.Contains("moisture") && value < 20)
                || (metric.Contains("power") && value > 1800)
                || (metric.Contains("temperature") && value > 42))) return 0;
        if (bool.TryParse(request.Value, out var connected) && !connected) return 1;
        return 2;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _available.WaitAsync(stoppingToken);
                if (!_pending.TryDequeue(out var work)) continue;
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<TelemetryService>();
                    work.Completion.TrySetResult(await service.ProcessTelemetryAsync(work.Request));
                }
                catch (Exception error) { work.Completion.TrySetException(error); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            while (_pending.TryDequeue(out var work)) work.Completion.TrySetCanceled();
        }
    }
}
