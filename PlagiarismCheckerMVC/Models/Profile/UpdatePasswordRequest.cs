using System.ComponentModel.DataAnnotations;

namespace PlagiarismCheckerMVC.Models.Profile
{
    public class UpdatePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
} 