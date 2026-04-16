using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartResume.Services.Interfaces;



    public class JobService : IJobService
    {private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    // HttpClient'ı constructor üzerinden enjekte et
    public JobService(IConfiguration configuration, HttpClient httpClient)
    {
        _apiKey = configuration["Jooble:ApiKey"]
           ?? throw new InvalidOperationException("Job API Key is missing");
        _httpClient = httpClient;
    }

   public async Task<string> GetJobRecommendationsAsync(List<string> keywords, string location)
{
    var requestUrl = $"https://jooble.org/api/{_apiKey}";

    var requestBody = new
    {
        keywords = string.Join(" ", keywords), // Kelimeleri boşlukla birleştir
        location = location,
        page = "1"
    };

    var jsonPayload = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

    var response = await _httpClient.PostAsync(requestUrl, content);

    if (!response.IsSuccessStatusCode)
    {
        throw new HttpRequestException($"Jooble Hatası: {response.StatusCode}");
    }

    return await response.Content.ReadAsStringAsync();
}
}