using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SmartResume.Services.Interfaces;
using System.Text;
using System.Text.Json;

public class GeminiService : IGeminiService
{
    private readonly string _apiKey;
    private readonly string _model;


    public GeminiService(IConfiguration configuration)
    {
        

        _apiKey = configuration["Gemini:ApiKey"]
           ?? throw new InvalidOperationException("Gemini ApiKey is missing");

        _model = configuration["Gemini:Model"]
                  ?? throw new InvalidOperationException("Gemini Model is missing");
    }

  
    public async Task<string> AnalyzeResumeAsync(string resumeText)
    {
        using var httpClient = new HttpClient();

        var requestUrl =
   $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";


        var prompt = @"You are a highly experienced HR specialist tasked with analyzing resumes. 
Extract information from the CV text below and return it ONLY in JSON format. Do not write anything else, only JSON.

Required output format:
{
  ""firstName"": ""First name"",
  ""lastName"": ""Last name"",
  ""address"": ""Address"",
  ""summary"": ""Short summary (2-3 sentences)"",
  ""skills"": [""Skill1"", ""Skill2"", ""Skill3""],
  ""languages"": [""Language1"", ""Language2""],
  ""experience"": [
    {
      ""company"": ""Company name"",
      ""title"": ""Job title"",
      ""startDate: ""dd.MM.yyyy"",
      ""endDate: ""dd.MM.yyyy""
    }
  ],
  ""education"": [
    {
      ""school"": ""School name"",
      ""degree"": ""Degree"",
      ""startDate: ""dd.MM.yyyy"",
      ""endDate: ""dd.MM.yyyy""
    }
  ],
  ""contactDetails"": {
    ""email"": ""Email address"",
    ""phone"": ""Phone number"",
    ""country"": ""Country""
    ""city"": ""City"",
    } 
}

If any field cannot be found, return an empty string or an empty array. Preserve the original language of the CV content as is.
Date rules:
- Use format ""dd.MM.yyyy"".
- If only the year is available, use ""01.01.YYYY"".
- If month and year are available but day is missing, use ""01.MM.YYYY"".
- If the position or education is ongoing, set endDate to an empty string """".
- Do NOT guess dates. Use only information explicitly present in the CV.
CV Text:
" + resumeText;
        

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(requestUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Hata durumunda detaylı mesaj fırlatıyoruz
                throw new Exception($"Gemini API Hatası ({response.StatusCode}): {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var rawText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Markdown temizliği
            rawText = rawText.Trim();
            if (rawText.StartsWith("```json")) rawText = rawText.Substring(7);
            if (rawText.StartsWith("```")) rawText = rawText.Substring(3);
            if (rawText.EndsWith("```")) rawText = rawText.Substring(0, rawText.Length - 3);

            return rawText.Trim();
        }
        catch (Exception ex)
        {
            // Tüm hata yollarında bir exception fırlatarak CS0161 hatasını engelliyoruz
            throw new Exception($"an error occured during analyzing resume: {ex.Message}");
        }
    }
}