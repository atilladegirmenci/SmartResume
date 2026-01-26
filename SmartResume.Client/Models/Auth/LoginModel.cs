using System.ComponentModel.DataAnnotations;

namespace SmartResume.Client.Models.Auth
{
    public class LoginModel
    {
       
        
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address format")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required")]
            public string Password { get; set; }
        
    }
}
