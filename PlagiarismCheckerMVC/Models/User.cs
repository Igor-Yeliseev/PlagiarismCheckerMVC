using System;
using System.ComponentModel.DataAnnotations;

namespace PlagiarismCheckerMVC.Models
{
    public enum UserRole
    {
        User,
        Admin
    }

    public class User
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string HashedPassword { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public UserRole Role { get; set; } = UserRole.User; // По умолчанию обычный пользователь
    }
} 