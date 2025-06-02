using System.ComponentModel.DataAnnotations;

namespace PlagiarismCheckerMVC.Models.Profile
{
    public class UpdatePhoneRequest
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
    }
} 