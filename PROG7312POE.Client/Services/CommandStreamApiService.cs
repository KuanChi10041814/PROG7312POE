using System.Net.Http.Json;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Client.Services;

public class CommandStreamApiService
{
    public CommandCreateRequest Draft { get; } = new();
    public string? SearchTerm { get; set; }

    private readonly HttpClient _httpClient;

    public CommandStreamApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CommandStreamDashboard> GetDashboardAsync(string? searchTerm)
    {
        var query = string.IsNullOrWhiteSpace(searchTerm)
            ? string.Empty
            : $"?searchTerm={Uri.EscapeDataString(searchTerm)}";
        return await _httpClient.GetFromJsonAsync<CommandStreamDashboard>($"api/command-stream{query}")
            ?? new CommandStreamDashboard();
    }

    public async Task<CommandResponse?> IssueCommandAsync(CommandCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/command-stream/commands", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CommandResponse>();
    }

    public async Task<CommandResponse?> UndoLastCommandAsync()
    {
        var response = await _httpClient.PostAsync("api/command-stream/commands/undo", null);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CommandResponse>();
    }
}
