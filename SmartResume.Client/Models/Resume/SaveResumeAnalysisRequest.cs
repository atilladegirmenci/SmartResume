
namespace SmartResume.Client.Models.Resume
{
    public class SaveResumeAnalysisRequest
    {
        public List<string> Skills { get; set; } = new();
        public List<SaveEducationDto> Education { get; set; } = new();
        public List<SaveExperianceDto> Experience { get; set; } = new();

        public SaveContactDetailsDto ContactDetails { get; set; } = new();
    }
}
