using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartResume.Data;
using SmartResume.Data.Models;
using SmartResume.DTOs.Requests;
using SmartResume.DTOs.Responses;
using SmartResume.Services.Interfaces;
using System.Security.Claims;

namespace SmartCV.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IOcrService _ocrService;
        private readonly IGeminiService _geminiService;

        public ResumeController(ApplicationDbContext context, IWebHostEnvironment environment, IOcrService ocrService, IGeminiService geminiService)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (ocrService == null) throw new ArgumentNullException(nameof(ocrService));
            if (geminiService == null) throw new ArgumentNullException(nameof(geminiService));

            _context = context;
            _environment = environment;
            _ocrService = ocrService;
            _geminiService = geminiService;
        }

        [HttpPost("upload")]
        [Authorize]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found.");

            //int userId = int.Parse(userIdClaim);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                Console.WriteLine($"User claim int dönüştürülemedi: {userIdClaim}");
                return Unauthorized("Invalid user ID.");
            }
            Console.WriteLine($"UserID: {userId}");

            try
            {
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

                return Ok(new { message = "Upload successful", resumeId = resume.ResumeID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error during cv upload: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResumeResponse>>> GetMyResumes()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            var resumes = await _context.Resumes
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.UploadedAt)
                .Select(r => new ResumeResponse
                {
                    ResumeID = r.ResumeID,
                    UserGivenTitle = r.UserGivenTitle,
                    OriginalFileName = r.OriginalFileName,
                    UploadedAt = r.UploadedAt
                })
                .ToListAsync();

            return Ok(resumes);
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> GetResumeFile(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeID == id && r.UserID == userId);
            if (resume == null) return NotFound("Resume not found.");
            if (!System.IO.File.Exists(resume.StoragePath)) return NotFound("Physical file not found.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(resume.StoragePath);
            return File(fileBytes, "application/pdf", resume.OriginalFileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResume(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeID == id && r.UserID == userId);
            if (resume == null) return NotFound();

            try
            {
                _context.Resumes.Remove(resume);
                await _context.SaveChangesAsync();

                if (System.IO.File.Exists(resume.StoragePath))
                    System.IO.File.Delete(resume.StoragePath);

                return Ok(new { message = "Resume deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting resume: {ex.Message}");
            }
        }

        [HttpPost("analyze/{id}")]
        public async Task<IActionResult> AnalyzeResume(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeID == id && r.UserID == userId);
            if (resume == null) return NotFound("Resume not found in database.");
            if (!System.IO.File.Exists(resume.StoragePath)) return NotFound("Physical resume file not found.");

            try
            {
                // 1. OCR ile text çıkar
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(resume.StoragePath);
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


                return Ok(analysisResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Analysis failed: {ex.Message}" });
            }
        }


        [HttpPost("{resumeId}/analysis")]
        public async Task<IActionResult> SaveAnalysis(int resumeId, [FromBody] SaveResumeAnalysisRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request body.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var resume = await _context.Resumes
                .Include(r => r.ResumeSkills)
                .Include(r => r.Educations)
                .Include(r => r.Experiences)
                .FirstOrDefaultAsync(r => r.ResumeID == resumeId && r.UserID == userId);

            if (resume == null)
                return NotFound("Resume not found.");

            // ----- SKILLS -----

            _context.ResumeSkills.RemoveRange(resume.ResumeSkills);

            var normalizedSkills = (request.Skills ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLower())
                .Distinct()
                .ToList();

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

            var allSkills = existingSkills.Concat(newSkills).ToList();

            foreach (var skill in allSkills)
            {
                resume.ResumeSkills.Add(new ResumeSkill
                {
                    ResumeID = resume.ResumeID,
                    Skill = skill
                });
            }

            // ----- EDUCATION -----

            _context.Educations.RemoveRange(resume.Educations);

            var educationEntities = request.Education.Select(e => new Education
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

            var experienceEntities = request.Experience.Select(e => new Experience
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

            
            var contactInfoEntities = new ContactDetail()
            {
                ResumeID = resume.ResumeID,
                Email = request.ContactDetails.Email,
                PhoneNumber = request.ContactDetails.Phone,
                City = request.ContactDetails.City,
                Country = request.ContactDetails.Country

            };

            await _context.ContactDetails.AddAsync(contactInfoEntities);
            // ----- METADATA -----

            resume.LastAnalyzedAt = DateTime.UtcNow;
            resume.UploadedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok();
        }

    }
}