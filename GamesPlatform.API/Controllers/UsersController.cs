using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context) => _context = context;

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            foreach (var u in users) u.PasswordHash = "";
            return users;
        }

        // Получение пользователя по ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.PasswordHash = "";
            return user;
        }

        // Смена роли пользователя
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserType))
                return BadRequest("Роль не может быть пустой");
            
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Пользователь не найден");
            
            user.UserType = dto.UserType;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Роль обновлена", userName = user.UserName, userType = user.UserType });
        }

        // Блокировка/Разблокировка пользователя
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Пользователь не найден");
            
            user.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = $"Пользователь {(dto.IsActive ? "разблокирован" : "заблокирован")}", isActive = user.IsActive });
        }

        // Смена ника
        [HttpPut("me/username")]
        [Authorize]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest("Имя не может быть пустым");
            
            dto.UserName = dto.UserName.Trim();
            if (dto.UserName.Length < 3 || dto.UserName.Length > 50)
                return BadRequest("Имя должно быть от 3 до 50 символов");

            var exists = await _context.Users.AnyAsync(u => u.UserName == dto.UserName && u.UserId != userId);
            if (exists) return BadRequest("Имя уже занято");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.UserName = dto.UserName;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Имя обновлено", userName = user.UserName });
        }

        // Смена даты рождения
        [HttpPut("me/dateofbirth")]
        [Authorize]
        public async Task<IActionResult> UpdateDateOfBirth([FromBody] UpdateDateOfBirthDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value > DateTime.UtcNow)
                return BadRequest("Дата рождения не может быть в будущем");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.DateOfBirth = dto.DateOfBirth;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Дата рождения обновлена", dateOfBirth = user.DateOfBirth });
        }

        // Регистрация
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            user.RegistrationDate = DateTime.UtcNow;
            user.IsActive = true;
            user.UserType = "User"; 
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            user.PasswordHash = "";
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        // Удаление пользователя
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class UpdateUsernameDto { [Required] public string UserName { get; set; } = string.Empty; }
    public class UpdateDateOfBirthDto { public DateTime? DateOfBirth { get; set; } }
    public class UpdateRoleDto { [Required] public string UserType { get; set; } = string.Empty; }
    public class UpdateStatusDto { public bool IsActive { get; set; } }
}