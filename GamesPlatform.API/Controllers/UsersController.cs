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

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // Получение списка всех пользователей
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            
            foreach (var user in users)
            {
                user.PasswordHash = "";
            }
            
            return users;
        }

        // Получение пользователя по ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.PasswordHash = "";

            return user;
        }

        // Обновление данных пользователя
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest("ID в URL и теле запроса не совпадают");
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // Смена ника текущего пользователя
        [HttpPut("me/username")]
        [Authorize]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не удалось определить пользователя из токена");
            }

            if (string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest("Имя пользователя не может быть пустым");
            
            dto.UserName = dto.UserName.Trim();
            
            if (dto.UserName.Length < 3)
                return BadRequest("Имя пользователя должно содержать минимум 3 символа");
            
            if (dto.UserName.Length > 50)
                return BadRequest("Имя пользователя не должно превышать 50 символов");

            var exists = await _context.Users.AnyAsync(u => u.UserName == dto.UserName && u.UserId != userId);
            if (exists)
            {
                return BadRequest("Пользователь с таким именем уже существует");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Пользователь не найден");
            }

            user.UserName = dto.UserName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Имя пользователя успешно обновлено", userName = user.UserName });
        }

        // Обновление даты рождения текущего пользователя
        [HttpPut("me/dateofbirth")]
        [Authorize]
        public async Task<IActionResult> UpdateDateOfBirth([FromBody] UpdateDateOfBirthDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не удалось определить пользователя из токена");
            }

            if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value > DateTime.UtcNow)
            {
                return BadRequest("Дата рождения не может быть в будущем");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Пользователь не найден");
            }

            user.DateOfBirth = dto.DateOfBirth;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Дата рождения успешно обновлена", dateOfBirth = user.DateOfBirth });
        }

        // Регистрация нового пользователя
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            user.RegistrationDate = DateTime.UtcNow;
            user.IsActive = true;
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = user;
            result.PasswordHash = "";
            
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, result);
        }

        // Удаление пользователя
        // Только администраторы
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }

    // DTO для запроса смены имени пользователя
    public class UpdateUsernameDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
    }

    // DTO для запроса смены даты рождения
    public class UpdateDateOfBirthDto
    {
        public DateTime? DateOfBirth { get; set; }
    }
}