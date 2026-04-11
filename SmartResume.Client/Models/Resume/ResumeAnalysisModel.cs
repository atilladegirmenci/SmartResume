namespace SmartResume.Client.Models.Resume
{
    public class ResumeAnalysisModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
       // public string Email { get; set; } = "";
       // public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
       // public string City { get; set; } = "";
       // public string Country { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> Skills { get; set; } = new();
        public List<string> SelectedSkills { get; set; } = new();
        public List<string> Languages { get; set; } = new();
        public List<ExperienceItemModel> Experience { get; set; } = new();
        public List<EducationItemModel> Education { get; set; } = new();
        public ContactDetails ContactDetails { get; set; } = new();
    }

    public class ContactDetails
    {
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string City { get; set; } = "";
        public string Country { get; set; } = "";

    }
    public class ExperienceItemModel
    {
        public string Company { get; set; } = "";
        public string Title { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
    }

    public class EducationItemModel
    {
        public string School { get; set; } = "";
        public string Degree { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
    }
}