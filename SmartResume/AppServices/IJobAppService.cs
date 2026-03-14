using System.Collections.Generic;
using System.Threading.Tasks;
using SmartResume.DTOs.Responses;

namespace SmartResume.AppServices
{
    public interface IJobAppService
    {
        Task<List<JobListingDto>> GetRecommendationsForResumeAsync(int resumeId, int userId);
    }
}