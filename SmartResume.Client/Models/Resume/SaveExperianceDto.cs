namespace SmartResume.Client.Models.Resume
{
    public class SaveExperianceDto
    {
        public string? PositionTitle { get; set; } // e.g., 'Senior Software Engineer'
        public string? CompanyName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; } // Null if currently working here
        public string? Description { get; set; } // Job description text
    }
}
