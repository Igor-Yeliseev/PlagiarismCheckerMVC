using System;
using System.Threading.Tasks;
using PlagiarismCheckerMVC.Models;

namespace PlagiarismCheckerMVC.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        string GenerateJwtToken(User user);
    }
} 