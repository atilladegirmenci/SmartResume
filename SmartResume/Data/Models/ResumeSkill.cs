using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartResume.Data.Models
{
    // This is the junction (linking) table for the Many-to-Many relationship between Resumes and Skills.
    public class ResumeSkill
    {
        [Key]
        public int ResumeSkillID { get; set; }

        [Required]
        public int ResumeID { get; set; } // Foreign key to Resume

        [Required]
        public int SkillID { get; set; } // Foreign key to Skill

        // --- Navigation Properties ---

        [ForeignKey("ResumeID")]
        public virtual Resume Resume { get; set; }

        [ForeignKey("SkillID")]
        public virtual Skill Skill { get; set; }
    }
}
