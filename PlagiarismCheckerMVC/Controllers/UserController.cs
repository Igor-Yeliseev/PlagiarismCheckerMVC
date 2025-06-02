using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlagiarismCheckerMVC.Models;
using PlagiarismCheckerMVC.Models.Profile;

namespace PlagiarismCheckerMVC.Controllers
{
    [Route("plag-api/user")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
            if (user == null)
                return NotFound();

            var profileView = new ProfileView
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role == UserRole.Admin ? "Администратор" : "Пользователь",
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            };

            return Ok(profileView);
        }

        [HttpPut("username")]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(new { message = "Имя пользователя не может быть пустым" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
            if (user == null)
                return NotFound();

            user.Username = request.Username;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Имя пользователя успешно обновлено" });
        }

        [HttpPut("password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { message = "Пароли не могут быть пустыми" });

            if (request.NewPassword.Length < 6)
                return BadRequest(new { message = "Новый пароль должен содержать минимум 6 символов" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
            if (user == null)
                return NotFound();

            // Проверяем текущий пароль
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.HashedPassword))
                return BadRequest(new { message = "Неверный текущий пароль" });

            // Хешируем и сохраняем новый пароль
            user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Пароль успешно обновлен" });
        }

        [HttpPut("phone")]
        public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return BadRequest(new { message = "Номер телефона не может быть пустым" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
            if (user == null)
                return NotFound();

            user.PhoneNumber = request.PhoneNumber;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Номер телефона успешно обновлен" });
        }
    }
}