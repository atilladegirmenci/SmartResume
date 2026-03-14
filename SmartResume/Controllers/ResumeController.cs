using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartResume.AppServices;
using SmartResume.DTOs.Requests;
using SmartResume.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SmartCV.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeController : ControllerBase
    {
        private readonly IResumeAppService _resumeAppService;
        private readonly IJobAppService _jobAppService;

        public ResumeController(IResumeAppService resumeAppService, IJobAppService jobAppService)
        {
            _resumeAppService = resumeAppService ?? throw new ArgumentNullException(nameof(resumeAppService));
            _jobAppService = jobAppService ?? throw new ArgumentNullException(nameof(jobAppService));
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User ID not found.");

            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Invalid user ID.");

            return userId;
        }

        [HttpPost("upload")]
        [Authorize]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            try
            {
                int userId = GetUserId();
                var resumeId = await _resumeAppService.UploadResumeAsync(file, userId);
                return Ok(new { message = "Upload successful", resumeId = resumeId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error during cv upload: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResumeResponse>>> GetMyResumes()
        {
            try
            {
                int userId = GetUserId();
                var resumes = await _resumeAppService.GetMyResumesAsync(userId);
                return Ok(resumes);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> GetResumeFile(int id)
        {
            try
            {
                int userId = GetUserId();
                var fileData = await _resumeAppService.GetResumeFileAsync(id, userId);
                return File(fileData.FileBytes, fileData.ContentType, fileData.OriginalFileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResume(int id)
        {
            try
            {
                int userId = GetUserId();
                await _resumeAppService.DeleteResumeAsync(id, userId);
                return Ok(new { message = "Resume deleted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting resume: {ex.Message}");
            }
        }

        [HttpPost("analyze/{id}")]
        public async Task<IActionResult> AnalyzeResume(int id)
        {
            try
            {
                int userId = GetUserId();
                var analysisResult = await _resumeAppService.AnalyzeResumeAsync(id, userId);
                return Ok(analysisResult);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Analysis failed: {ex.Message}" });
            }
        }

        [HttpPost("{resumeId}/analysis")]
        public async Task<IActionResult> SaveAnalysis(int resumeId, [FromBody] SaveResumeAnalysisRequest request)
        {
            try
            {
                int userId = GetUserId();
                await _resumeAppService.SaveAnalysisAsync(resumeId, userId, request);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{resumeId}/job-recommendations")]
        public async Task<IActionResult> GetJobRecommendations(int resumeId)
        {
            try
            {
                int userId = GetUserId();
                var jobList = await _jobAppService.GetRecommendationsForResumeAsync(resumeId, userId);
                
                // Log the results to the console so we can verify during testing
                Console.WriteLine($"\n=== SUCCESS: FOUND {jobList?.Count ?? 0} JOBS ===");
                var jsonLogs = System.Text.Json.JsonSerializer.Serialize(jobList, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(jsonLogs);
                Console.WriteLine("==================================================\n");

                return Ok(jobList);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Resume not found.")
                    return NotFound(ex.Message);
                if (ex.Message.Contains("analyzed before getting job recommendations") || ex.Message.Contains("No skills"))
                    return BadRequest(ex.Message);

                Console.WriteLine($"[JobAppService] Error: {ex.Message}");
                return StatusCode(500, $"Error fetching job recommendations: {ex.Message}");
            }
        }
    }
}