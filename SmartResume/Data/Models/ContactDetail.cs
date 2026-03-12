using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartResume.Data.Models
{
    /// <summary>
    /// Represents the personal and contact information extracted from the resume header.
    /// This keeps the main Resume class clean from domain-specific data.
    /// </summary>
    public class ContactDetail
    {
        [Key]
        public int ContactDetailID { get; set; }

        // --- Extracted Location Info ---
        [StringLength(100)]
        public string? City { get; set; } // The specific field you asked for

        [StringLength(100)]
        public string? Country { get; set; }

        // --- Extracted Contact Info ---
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string? Email { get; set; } // The email written on the CV (might differ from User account email)

        //[StringLength(500)]
        //public string? LinkedInUrl { get; set; }

        //[StringLength(500)]
        //public string? PortfolioUrl { get; set; }

        // --- Relationship ---
        [Required]
        public int ResumeID { get; set; }

        [ForeignKey("ResumeID")]
        public virtual Resume Resume { get; set; } = null!;
    }
}