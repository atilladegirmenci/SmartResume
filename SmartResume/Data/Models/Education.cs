using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartResume.Data.Models
{
    public class Education
    {
        [Key]
        public int EducationID { get; set; }

        [Required]
        public int ResumeID { get; set; } // Foreign key to the Resume this education belongs to

        [StringLength(250)]
        public string? InstitutionName { get; set; } // e.g., 'Istanbul Technical University'

        [StringLength(250)]
        public string? Degree { get; set; } // e.g., 'B.Sc. in Computer Engineering'

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; } // Null if currently studying

        // --- Navigation Properties ---
        [ForeignKey("ResumeID")]
        public virtual Resume Resume { get; set; }
    }
}
