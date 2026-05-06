using Microsoft.Extensions.Configuration;
using SmartResume.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;



    public class JobService : IJobService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public JobService(IConfiguration configuration, HttpClient httpClient)
        {
            _apiKey = configuration["Jooble:ApiKey"]
                    ?? throw new InvalidOperationException("Job API Key is missing");
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> GetJobRecommendationsAsync(List<string> keywords, string location)
        {
            var requestUrl = $"https://tr.jooble.org/api/{_apiKey}";

            Console.WriteLine($"[JobService] Keywords sent to Jooble: {string.Join(", ", keywords)}");
            Console.WriteLine($"[JobService] Location sent to Jooble: {location}");

            var requestBody = new
            {
                keywords = string.Join(" ", keywords),
                location = location,
                page = "1"
            };

            Console.WriteLine($"[JobService] Request payload keywords text: {requestBody.keywords}");

            var jsonContent = JsonContent.Create(requestBody);
            var response = await _httpClient.PostAsync(requestUrl, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Jooble API request failed with status code: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
    }
}