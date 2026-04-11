using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartResume.Data;
using SmartResume.Data.Models;
using SmartResume.DTOs.Requests;
using SmartResume.DTOs.Responses;
using SmartResume.Services.Interfaces;

namespace SmartResume.AppServices
{
    public class ResumeAppService : IResumeAppService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IOcrService _ocrService;
        private readonly IGeminiService _geminiService;

        public ResumeAppService(
            ApplicationDbContext context, 
            IWebHostEnvironment environment, 
            IOcrService ocrService, 
            IGeminiService geminiService)
        {
            _context = context;
            _environment = environment;
            _ocrService = ocrService;
            _geminiService = geminiService;
        }

        public async Task<int> UploadResumeAsync(IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            string rootPath = _environment.ContentRootPath;
            string uploadsFolder = Path.Combine(rootPath, "UploadedCVs");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var resume = new Resume
            {
                UserID = userId,
                OriginalFileName = file.FileName,
                StoragePath = filePath,
                UserGivenTitle = file.FileName,
                UploadedAt = DateTime.UtcNow
            };

            _context.Resumes.Add(resume);
            await _context.SaveChangesAsync();

            return resume.ResumeID;
        }

        public async Task<IEnumerable<ResumeResponse>> GetMyResumesAsync(int userId)
        {
            return await _context.Resumes
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.UploadedAt)
                .Select(r => new ResumeResponse
                {
                    ResumeID = r.ResumeID,
                    UserGivenTitle = r.UserGivenTitle,
                    OriginalFileName = r.OriginalFileName,
                    UploadedAt = r.UploadedAt,
                    IsAnalyzed = r.IsAnalyzed
                })
                .ToListAsync();
        }

        public async Task<(byte[] FileBytes, string ContentType, string OriginalFileName)> GetResumeFileAsync(int resumeId, int userId)
        {
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeID == resumeId && r.UserID == userId);
            if (resume == null) throw new KeyNotFoundException("Resume not found.");
            if (!File.Exists(resume.StoragePath)) throw new FileNotFoundException("Physical file not found.");

            var fileBytes = await File.ReadAllBytesAsync(resume.StoragePath);
            return (fileBytes, "application/pdf", resume.OriginalFileName ?? "resume.pdf");
        }

        public async Task DeleteResumeAsync(int resumeId, int userId)
        {
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeID == resumeId && r.UserID == userId);
            if (resume == null) throw new KeyNotFoundException("Resume not found.");

            _context.Resumes.Remove(resume);
            await _context.SaveChangesAsync();

            if (File.Exists(resume.StoragePath))
                File.Delete(resume.StoragePath);
        }

        public async Task UpdateResumeTitleAsync(int resumeId, int userId, string userGivenTitle)
        {
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeID == resumeId && r.UserID == userId);
            if (resume == null) throw new KeyNotFoundException("Resume not found.");

            var trimmedTitle = (userGivenTitle ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedTitle))
                throw new ArgumentException("Title cannot be empty.");

            if (trimmedTitle.Length > 200)
                throw new ArgumentException("Title cannot be longer than 200 characters.");

            resume.UserGivenTitle = trimmedTitle;
            await _context.SaveChangesAsync();
        }

        public async Task<ResumeAnalysisResponse> AnalyzeResumeAsync(int resumeId, int userId)
        {
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeID == resumeId && r.UserID == userId);
            if (resume == null) throw new KeyNotFoundException("Resume not found in database.");
            if (!File.Exists(resume.StoragePath)) throw new FileNotFoundException("Physical resume file not found.");

            // 1. OCR ile text çıkar
            byte[] fileBytes = await File.ReadAllBytesAsync(resume.StoragePath);
            string rawText = _ocrService.ExtractTextFromPdf(fileBytes);
            resume.ExtractedRawText = rawText;

            // 2. Gemini ile analiz yap (JSON string döner)
            string analysisJson = await _geminiService.AnalyzeResumeAsync(rawText);
            resume.AnalysisResult = analysisJson;
            resume.LastAnalyzedAt = DateTime.UtcNow;

            // 3. Kaydet
            _context.Resumes.Update(resume);
            await _context.SaveChangesAsync();

            // 4. JSON'u parse edip DTO olarak döndür
            var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ResumeAnalysisResponse>(
                analysisJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (analysisResult == null)
                throw new InvalidOperationException("Failed to deserialize analysis result.");

            return analysisResult;
        }

        public async Task<ResumeAnalysisResponse> GetSavedAnalysisAsync(int resumeId, int userId)
        {
            var resume = await _context.Resumes
                .Include(r => r.ResumeSkills)
                    .ThenInclude(rs => rs.Skill)
                .Include(r => r.Educations)
                .Include(r => r.Experiences)
                .FirstOrDefaultAsync(r => r.ResumeID == resumeId && r.UserID == userId);

            if (resume == null)
                throw new KeyNotFoundException("Resume not found.");

            if (!resume.IsAnalyzed)
                throw new InvalidOperationException("Resume has not been analyzed yet.");

            ResumeAnalysisResponse response;

            if (!string.IsNullOrWhiteSpace(resume.AnalysisResult))
            {
                response = System.Text.Json.JsonSerializer.Deserialize<ResumeAnalysisResponse>(
                    resume.AnalysisResult,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new ResumeAnalysisResponse();
            }
            else
            {
                response = new ResumeAnalysisResponse();
            }

            response.Skills = resume.ResumeSkills
                .Where(rs => rs.Skill != null && !string.IsNullOrWhiteSpace(rs.Skill.SkillName))
                .OrderBy(rs => rs.Importance)
                .Select(rs => rs.Skill.SkillName)
                .Distinct()
                .ToList();

            response.SelectedSkills = resume.ResumeSkills
                .Where(rs => rs.IsSelectedForSearch && rs.Skill != null && !string.IsNullOrWhiteSpace(rs.Skill.SkillName))
                .OrderBy(rs => rs.Importance)
                .Select(rs => rs.Skill.SkillName)
                .Distinct()
                .ToList();

            response.Education = resume.Educations
                .OrderByDescending(e => e.EndDate ?? DateTime.MinValue)
                .Select(e => new EducationItem
                {
                    School = e.InstitutionName ?? string.Empty,
                    Degree = e.Degree ?? string.Empty,
                    StartDate = FormatDateOrEmpty(e.StartDate),
                    EndDate = FormatDateOrEmpty(e.EndDate)
                })
                .ToList();

            response.Experience = resume.Experiences
                .OrderByDescending(e => e.EndDate ?? DateTime.MinValue)
                .Select(e => new ExperienceItem
                {
                    Company = e.CompanyName ?? string.Empty,
                    Title = e.PositionTitle ?? string.Empty,
                    StartDate = FormatDateOrEmpty(e.StartDate),
                    EndDate = FormatDateOrEmpty(e.EndDate)
                })
                .ToList();

            var contact = await _context.ContactDetails
                .Where(cd => cd.ResumeID == resumeId)
                .OrderByDescending(cd => cd.ContactDetailID)
                .FirstOrDefaultAsync();

            response.ContactDetails ??= new DTOs.Responses.ContactDetails();
            response.ContactDetails.Email = contact?.Email ?? string.Empty;
            response.ContactDetails.Phone = contact?.PhoneNumber ?? string.Empty;
            response.ContactDetails.City = contact?.City ?? string.Empty;
            response.ContactDetails.Country = contact?.Country ?? string.Empty;

            return response;
        }

        public async Task SaveAnalysisAsync(int resumeId, int userId, SaveResumeAnalysisRequest request)
        {
            if (request == null)
                throw new ArgumentException("Invalid request body.");

            var resume = await _context.Resumes
                .Include(r => r.ResumeSkills)
                .Include(r => r.Educations)
                .Include(r => r.Experiences)
                .FirstOrDefaultAsync(r => r.ResumeID == resumeId && r.UserID == userId);

            if (resume == null)
                throw new KeyNotFoundException("Resume not found.");

            // ----- SKILLS -----
            _context.ResumeSkills.RemoveRange(resume.ResumeSkills);

            var normalizedSkills = (request.Skills ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLower())
                .Distinct()
                .ToList();

            var selectedSkills = (request.SelectedSkills ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLower())
                .Distinct()
                .ToHashSet();

            var existingSkills = await _context.Skills
                .Where(s => normalizedSkills.Contains(s.SkillName))
                .ToListAsync();

            var newSkillNames = normalizedSkills
                .Except(existingSkills.Select(s => s.SkillName))
                .ToList();

            var newSkills = newSkillNames.Select(name => new Skill
            {
                SkillName = name
            }).ToList();

            _context.Skills.AddRange(newSkills);

            var skillsByName = existingSkills
                .Concat(newSkills)
                .ToDictionary(s => s.SkillName, s => s);

            for (int i = 0; i < normalizedSkills.Count; i++)
            {
                var skillName = normalizedSkills[i];
                if (!skillsByName.TryGetValue(skillName, out var skill))
                {
                    continue;
                }

                resume.ResumeSkills.Add(new ResumeSkill
                {
                    ResumeID = resume.ResumeID,
                    Skill = skill,
                    Importance = i + 1,
                    IsSelectedForSearch = selectedSkills.Count == 0
                        ? i < 3
                        : selectedSkills.Contains(skillName)
                });
            }

            // ----- EDUCATION ----- 
            _context.Educations.RemoveRange(resume.Educations);

            var educationEntities = (request.Education ?? new List<SaveEducationDto>()).Select(e => new Education
            {
                ResumeID = resume.ResumeID,
                InstitutionName = e.InstitutionName,
                Degree = e.Degree,
                StartDate = e.StartDate,
                EndDate = e.EndDate
            });

            await _context.Educations.AddRangeAsync(educationEntities);

            // ----- EXPERIENCE -----
            _context.Experiences.RemoveRange(resume.Experiences);

            var experienceEntities = (request.Experience ?? new List<SaveExperianceDto>()).Select(e => new Experience
            {
                ResumeID = resume.ResumeID,
                CompanyName = e.CompanyName,
                PositionTitle = e.PositionTitle,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Description = e.Description
            });

            await _context.Experiences.AddRangeAsync(experienceEntities);

            // ----- CONTACT INFO -----
            // To prevent duplicates, we might want to clean up existing ContactDetails or update it.
            // Assuming this is a one-to-one or similar mapping.
            var existingContact = await _context.ContactDetails.FirstOrDefaultAsync(cd => cd.ResumeID == resumeId);
            if (existingContact != null)
            {
                _context.ContactDetails.Remove(existingContact);
            }

            if (request.ContactDetails != null)
            {
                var contactInfoEntity = new ContactDetail()
                {
                    ResumeID = resume.ResumeID,
                    Email = request.ContactDetails.Email,
                    PhoneNumber = request.ContactDetails.Phone,
                    City = request.ContactDetails.City,
                    Country = request.ContactDetails.Country
                };
                await _context.ContactDetails.AddAsync(contactInfoEntity);
            }

            // ----- METADATA -----
            resume.LastAnalyzedAt = DateTime.UtcNow;
            resume.UploadedAt = DateTime.UtcNow;
            resume.IsAnalyzed = true;
            
            await _context.SaveChangesAsync();
        }

        private static string FormatDateOrEmpty(DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;
        }
    }
}