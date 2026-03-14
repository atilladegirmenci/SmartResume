using System.ComponentModel.DataAnnotations;

namespace SmartResume.DTOs.Responses
{
    public class ResumeResponse
    {
        public int ResumeID { get; set; }
        public string UserGivenTitle { get; set; } // e.g. "Myfile.pdf"
        public string OriginalFileName { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsAnalyzed { get; set; }
    }
}
