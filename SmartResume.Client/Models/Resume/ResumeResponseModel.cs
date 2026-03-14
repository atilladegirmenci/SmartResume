namespace SmartResume.Client.Models.Resume
{
    public class ResumeResponseModel
    {
        public int ResumeID { get; set; }
        public string UserGivenTitle { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public bool IsAnalyzed { get; set; }
    }
}
