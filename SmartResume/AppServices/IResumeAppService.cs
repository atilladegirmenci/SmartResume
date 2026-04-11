using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SmartResume.DTOs.Requests;
using SmartResume.DTOs.Responses;

namespace SmartResume.AppServices
{
    public interface IResumeAppService
    {
        Task<int> UploadResumeAsync(IFormFile file, int userId);
        Task<IEnumerable<ResumeResponse>> GetMyResumesAsync(int userId);
        Task<(byte[] FileBytes, string ContentType, string OriginalFileName)> GetResumeFileAsync(int resumeId, int userId);
        Task DeleteResumeAsync(int resumeId, int userId);
        Task UpdateResumeTitleAsync(int resumeId, int userId, string userGivenTitle);
        Task<ResumeAnalysisResponse> AnalyzeResumeAsync(int resumeId, int userId);
        Task<ResumeAnalysisResponse> GetSavedAnalysisAsync(int resumeId, int userId);
        Task SaveAnalysisAsync(int resumeId, int userId, SaveResumeAnalysisRequest request);
    }
}