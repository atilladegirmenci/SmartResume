using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartResume.Data;
using SmartResume.Data.Models;
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

        public ResumeController(ApplicationDbContext context, IWebHostEnvironment environment, IOcrService ocrService)
        {
            _context = context;
            _environment = environment;
            _ocrService = ocrService;
        }

        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found.");

            int userId = int.Parse(userIdClaim);

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
                return StatusCode(500, $"Server error: {ex.Message}");
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
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(resume.StoragePath);
                string rawText = _ocrService.ExtractTextFromPdf(fileBytes);

                resume.ExtractedRawText = rawText;

                _context.Resumes.Update(resume);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Analysis successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"OCR failed: {ex.Message}");
            }
        }
    }
}
