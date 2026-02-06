using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartResume.Data.Models
{
    public class Resume
    {
        [Key]
        public int ResumeID { get; set; }

        [Required]
        public int UserID { get; set; } // Foreign key for the User who owns this resume

        [StringLength(200)]
        public string UserGivenTitle { get; set; } = "New Resume"; // A friendly name given by the user

        [StringLength(255)]
        public string? OriginalFileName { get; set; } // e.g., 'john_doe_cv_2025.pdf' (users files original name)

        [Required]
        [StringLength(1024)]
        public string StoragePath { get; set; } // The path/URL where the actual PDF file is stored

        // Performance Optimization: Store the raw text extracted by Tesseract
        public string? ExtractedRawText { get; set; }
        public string? AnalysisResult { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAnalyzedAt { get; set; } // To track when it was last processed by an ML model

        // The User who owns this Resume
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        // The Skills extracted from this Resume
        public virtual ICollection<ResumeSkill> ResumeSkills { get; set; } = new List<ResumeSkill>();

        // The Experiences extracted from this Resume
        public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();

        // The Educations extracted from this Resume
        public virtual ICollection<Education> Educations { get; set; } = new List<Education>();
    }
}
