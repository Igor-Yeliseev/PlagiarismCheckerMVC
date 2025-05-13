using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlagiarismCheckerMVC.Models;

namespace PlagiarismCheckerMVC.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Проверяем, существует ли пользователь с таким email
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new InvalidOperationException("Пользователь с таким email уже существует");
            }

            // Хэшируем пароль
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Создаем нового пользователя
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                HashedPassword = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                Role = "user"
            };

            // Добавляем пользователя в базу данных
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Генерируем JWT токен
            var token = GenerateJwtToken(user);

            // Возвращаем ответ
            return new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Token = token
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Ищем пользователя по email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Проверяем, существует ли пользователь и правильный ли пароль
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.HashedPassword))
            {
                throw new InvalidOperationException("Неверный email или пароль");
            }

            // Генерируем JWT токен
            var token = GenerateJwtToken(user);

            // Возвращаем ответ
            return new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Token = token
            };
        }

        public string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 