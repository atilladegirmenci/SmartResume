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
using SmartResume.Data;
using SmartResume.Data.Models;
using SmartResume.Services.Interfaces;

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
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("User ID not found or invalid.");

            return userId;
        }

        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya seçilmedi.");

            try
            {
                int userId = GetUserId();

                // TÜM İŞ MANTIĞI BURADA: 
                // 1. R2'ye yükleme
                // 2. Veritabanına kayıt
                // Bu işlemler ResumeAppService.UploadResumeAsync içinde hallediliyor.
                var resumeId = await _resumeAppService.UploadResumeAsync(file, userId);

                return Ok(new { message = "Upload successful", resumeId = resumeId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Upload Error]: {ex.Message}");
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPut("{resumeId}/title")]
        public async Task<IActionResult> UpdateResumeTitle(int resumeId, [FromBody] UpdateResumeTitleRequest request)
        {
            try
            {
                int userId = GetUserId();
                await _resumeAppService.UpdateResumeTitleAsync(resumeId, userId, request?.UserGivenTitle ?? string.Empty);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
[HttpPost("{resumeId}/analysis")]
public async Task<IActionResult> SaveAnalysis(int resumeId, [FromBody] SaveResumeAnalysisRequest request)
{
    try
    {
        int userId = GetUserId();
        await _resumeAppService.SaveAnalysisAsync(resumeId, userId, request);
        return Ok(new { message = "Analysis saved successfully." });
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
    catch (Exception ex)
    {
        return StatusCode(500, $"Error saving analysis: {ex.Message}");
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Analysis failed: {ex.Message}" });
            }
        }

        [HttpGet("{resumeId}/analysis-result")]
        public async Task<IActionResult> GetSavedAnalysisResult(int resumeId)
        {
            try
            {
                int userId = GetUserId();
                var analysisResult = await _resumeAppService.GetSavedAnalysisAsync(resumeId, userId);
                return Ok(analysisResult);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{resumeId}/job-recommendations")]
        public async Task<IActionResult> GetJobRecommendations(int resumeId)
        {
            try
            {
                int userId = GetUserId();
                var jobList = await _jobAppService.GetRecommendationsForResumeAsync(resumeId, userId);
                return Ok(jobList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching job recommendations: {ex.Message}");
            }
        }
    }
}