using System;

namespace SmartResume.DTOs.Responses
{
    public class JobListingDto
    {
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
    }

    public class JoobleApiResponse
    {
        public List<JobListingDto> Jobs { get; set; } = new List<JobListingDto>();
    }
}
