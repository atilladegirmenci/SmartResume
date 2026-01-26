using System.ComponentModel.DataAnnotations;

namespace SmartResume.Data.Models
{
    public class Skill
    {
        // This is a lookup table for all unique skills (e.g., "Java", "Python", "SQL")

        [Key]
        public int SkillID { get; set; }

        [Required]
        [StringLength(100)]
        public string SkillName { get; set; } // Unique skill name

        // --- Navigation Properties ---
        // A list of all resume-skill relationships this skill participates in
        public virtual ICollection<ResumeSkill> ResumeSkills { get; set; } = new List<ResumeSkill>();

    }
}
