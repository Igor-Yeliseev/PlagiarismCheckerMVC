using System.ComponentModel.DataAnnotations;

namespace PlagiarismCheckerMVC.Models.Profile
{
    public class UpdateUsernameRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
    }
} 