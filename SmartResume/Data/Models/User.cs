using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartResume.Data.Models
{
    public class User
    {
        [Key] 
        public int UserID { get; set; }

        [Required(ErrorMessage = "email is required")] 
        [EmailAddress(ErrorMessage = "Please enter a valid email address")] // E-posta formatında olmasını zorunlu kılar
        [StringLength(255)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        public string LastName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Otomatik olarak oluşturma tarihini atar

        // İlişki: Bir kullanıcının birden fazla CV'si olabilir, ilk başta bu liste boş olacak kullanıcı cv yükledikçe dolacak
        public virtual ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    }
}
