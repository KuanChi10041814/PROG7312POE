using System.Net.Http.Json;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Client.Services;

public class SensorApiService
{
    private readonly HttpClient _httpClient;

    public SensorApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SensorResponse>> GetSensorsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<SensorResponse>>("api/sensors") ?? new();
    }

    public async Task<SensorResponse?> RegisterAsync(SensorCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/sensors", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorResponse>();
    }
}
