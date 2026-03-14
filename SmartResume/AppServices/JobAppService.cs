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
            if (resume == null)
            {
                throw new Exception("Resume not found.");
            }

            if (!resume.IsAnalyzed)
            {
                throw new Exception("Resume must be analyzed before getting job recommendations.");
            }

            Console.WriteLine($"[JobAppService] Fetching job recommendations for Resume ID: {resumeId}");

            // Get city from ContactDetail table
            var contactDetail = await _context.ContactDetails
                .FirstOrDefaultAsync(cd => cd.ResumeID == resumeId);

            string location = contactDetail?.City ?? "Turkey";
            Console.WriteLine($"[JobAppService] Location: {location}");

            // Get degrees from Education table
            var education = await _context.Educations
                .Where(e => e.ResumeID == resumeId)
                .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
                .FirstOrDefaultAsync();

            // Get skills from ResumeSkills table
            var skills = await _context.ResumeSkills
                .Where(rs => rs.ResumeID == resumeId)
                .Include(rs => rs.Skill)
                .Select(rs => rs.Skill.SkillName)
                .ToListAsync();

            // Build keywords list from skills and degree
            var keywords = new List<string>();
            keywords.AddRange(skills);
            if (!string.IsNullOrWhiteSpace(education?.Degree))
            {
                keywords.Add(education.Degree);
            }

            Console.WriteLine($"[JobAppService] Keywords: {string.Join(", ", keywords)}");

            if (keywords.Count == 0)
            {
                throw new Exception("No skills or education found in resume. Please analyze your resume first.");
            }

            // Call the external API service
            string jobRecommendationsJson = await _jobService.GetJobRecommendationsAsync(keywords, location);

            Console.WriteLine($"[JobAppService] Response received.");

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var joobleApiResp = System.Text.Json.JsonSerializer.Deserialize<JoobleApiResponse>(jobRecommendationsJson, options);

            return joobleApiResp?.Jobs ?? new List<JobListingDto>();
        }
    }
}