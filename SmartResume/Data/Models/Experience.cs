using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartResume.Data.Models
{
    public class Experience
    {
        [Key]
        public int ExperienceID { get; set; }

        [Required]
        public int ResumeID { get; set; } // Foreign key to the Resume this experience belongs to

        [StringLength(250)]
        public string? PositionTitle { get; set; } // e.g., 'Senior Software Engineer'

        [StringLength(250)]
        public string? CompanyName { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; } // Null if currently working here

        [Column(TypeName = "nvarchar(2000)")]
        public string? Description { get; set; } // Job description text

        // --- Navigation Properties ---
        [ForeignKey("ResumeID")]
        public virtual Resume Resume { get; set; }
    }
}
