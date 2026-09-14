using System.Net.Http.Json;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Client.Services;

public class TelemetryApiService
{
    private readonly HttpClient _httpClient;

    public TelemetryApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TelemetryResponse>> GetRecentAsync(int take = 50)
    {
        return await _httpClient.GetFromJsonAsync<List<TelemetryResponse>>($"api/telemetry?take={take}") ?? new();
    }

    public async Task<TelemetryResponse?> SendTelemetryAsync(TelemetryCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/telemetry", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TelemetryResponse>();
    }

    public async Task<EngagementSummary> GetEngagementSummaryAsync()
    {
        return await _httpClient.GetFromJsonAsync<EngagementSummary>("api/telemetry/engagement-summary")
            ?? new EngagementSummary();
    }
}
