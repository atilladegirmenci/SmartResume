using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartResume.Data;
using SmartResume.DTOs.Responses;
using SmartResume.Services.Interfaces;

namespace SmartResume.AppServices
{
    public class JobAppService : IJobAppService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJobService _jobService;

        public JobAppService(ApplicationDbContext context, IJobService jobService)
        {
            _context = context;
            _jobService = jobService;
        }

        public async Task<List<JobListingDto>> GetRecommendationsForResumeAsync(int resumeId, int userId)
        {
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeID == resumeId && r.UserID == userId);
            if (resume == null) throw new Exception("Resume not found.");
            if (!resume.IsAnalyzed) throw new Exception("Resume must be analyzed before getting job recommendations.");

            Console.WriteLine($"[JobAppService] Fetching job recommendations for Resume ID: {resumeId}");

            // 1. LOKASYON TEMİZLEME
            var contactDetail = await _context.ContactDetails.FirstOrDefaultAsync(cd => cd.ResumeID == resumeId);
            string location = (contactDetail?.City?.Length > 2) ? contactDetail.City : "Turkey";

            // 2. VERİLERİ TOPLAMA
            var skills = await _context.ResumeSkills
                .Where(rs => rs.ResumeID == resumeId && rs.IsSelectedForSearch)
                .Select(rs => rs.Skill.SkillName)
                .ToListAsync();

            var titles = await _context.Experiences
                .Where(e => e.ResumeID == resumeId)
                .Select(e => e.PositionTitle)
                .ToListAsync();

            var education = await _context.Educations
                .Where(e => e.ResumeID == resumeId)
                .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                .FirstOrDefaultAsync();

            var rawKeywords = new List<string>();
            rawKeywords.AddRange(titles); 
            rawKeywords.AddRange(skills);
            if (!string.IsNullOrWhiteSpace(education?.Degree)) rawKeywords.Add(education.Degree);

           // 3. ANAHTAR KELİMELERİ SADELEŞTİRME
var cleanKeywords = rawKeywords
    .Where(k => !string.IsNullOrWhiteSpace(k))
    .Select(k => k.Split('(')[0].Split('&')[0].Trim())
    .Where(k => k.Length > 2 && k.Length < 30)
    .Distinct()
    .ToList();

// --- BURADAN İTİBAREN DEĞİŞTİRİYORUZ ---
// Jooble için en etkili arama ünvanıdır.
var searchKeyword = titles.FirstOrDefault();

// EĞER ÜNVAN "Software Development" gibi eksikse veya boşsa, daha profesyonel bir hale getirelim
if (string.IsNullOrEmpty(searchKeyword) || searchKeyword.Equals("Software Development", StringComparison.OrdinalIgnoreCase))
{
    // İlk yeteneği al (Örn: C#, Java) ve yanına "Developer" ekle
    var topSkill = skills.FirstOrDefault() ?? "Software";
    searchKeyword = $"{topSkill} Engineer"; 
}
// Eğer ünvan çok uzunsa cleanKeywords içindeki ilk sade kelimeyi al
else if (searchKeyword.Length > 25)
{
    searchKeyword = cleanKeywords.FirstOrDefault() ?? "IT Specialist";
}

Console.WriteLine($"[JobAppService] Optimized Search Keyword for Jooble: {searchKeyword} in {location}");
// --- DEĞİŞİKLİK BURADA BİTTİ ---

// 4. API ÇAĞRISI (Aşağıdaki kısımlar aynı kalabilir)
string jobRecommendationsJson = await _jobService.GetJobRecommendationsAsync(new List<string> { searchKeyword }, location);
// Eğer sonuç boş gelirse (FOUND 0 JOBS durumu), lokasyonu genişletip tekrar dene
var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var joobleApiResp = System.Text.Json.JsonSerializer.Deserialize<JoobleApiResponse>(jobRecommendationsJson, options);

if (joobleApiResp?.Jobs == null || !joobleApiResp.Jobs.Any())
{
    Console.WriteLine($"[JobAppService] No jobs in {location}. Expanding search to Turkey...");
    jobRecommendationsJson = await _jobService.GetJobRecommendationsAsync(new List<string> { searchKeyword }, "Turkey");
    joobleApiResp = System.Text.Json.JsonSerializer.Deserialize<JoobleApiResponse>(jobRecommendationsJson, options);
}

return joobleApiResp?.Jobs ?? new List<JobListingDto>();
        }
    }
}