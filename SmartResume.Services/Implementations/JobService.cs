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
    {
        private readonly string _apiKey;
        private static readonly HttpClient _httpClient = new HttpClient();

        public JobService(IConfiguration configuration)
        {
            _apiKey = configuration["Jooble:ApiKey"]
               ?? throw new InvalidOperationException("Job API Key is missing");
        }

        public async Task<string> GetJobRecommendationsAsync(List<string> keywords, string location)
        {
            var requestUrl = $"https://tr.jooble.org/api/{_apiKey}";

            var requestBody = new
            {
                keywords = string.Join(" ", keywords),
                location = location,
                page = "1"
            };

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

