using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Client.Services;

public class AttachmentApiService
{
    private readonly HttpClient _httpClient;

    public AttachmentApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AttachmentResponse>> GetAttachmentsAsync(int sensorId)
    {
        return await _httpClient.GetFromJsonAsync<List<AttachmentResponse>>($"api/sensors/{sensorId}/attachments") ?? new();
    }

    public async Task<AttachmentResponse?> UploadAsync(int sensorId, IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(20_000_000));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await _httpClient.PostAsync($"api/sensors/{sensorId}/attachments", content);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AttachmentResponse>();
    }
}
