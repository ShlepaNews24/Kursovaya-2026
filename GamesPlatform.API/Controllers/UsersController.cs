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

        // ========================================================================
        // [НАЗНАЧЕНИЕ] Получение списка всех пользователей
        // [ДОСТУП] Только администраторы
        // ========================================================================
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            
            // [БЕЗОПАСНОСТЬ] Очищаем хеши паролей перед отправкой
            foreach (var user in users)
            {
                user.PasswordHash = "";
            }
            
            return users;
        }

        // ========================================================================
        // [НАЗНАЧЕНИЕ] Получение пользователя по ID
        // [ДОСТУП] Авторизованные пользователи
        // ========================================================================
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // [БЕЗОПАСНОСТЬ] Не возвращаем пароль
            user.PasswordHash = "";

            return user;
        }

        // ========================================================================
        // [НАЗНАЧЕНИЕ] Обновление данных пользователя
        // [ДОСТУП] Авторизованные (обычно админ или владелец профиля)
        // ========================================================================
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

        // ========================================================================
        // [НАЗНАЧЕНИЕ] Смена ника текущего пользователя
        // [МАРШРУТ] PUT /api/users/me/username
        // [ДОСТУП] Авторизованный пользователь
        // [БЕЗОПАСНОСТЬ] ID берётся из токена, проверка уникальности ника
        // ========================================================================
        [HttpPut("me/username")]
        [Authorize]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameDto dto)
        {
            // 1. Получаем ID пользователя из JWT-токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не удалось определить пользователя из токена");
            }

            // 2. Валидация нового ника
            if (string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest("Имя пользователя не может быть пустым");
            
            dto.UserName = dto.UserName.Trim();
            
            if (dto.UserName.Length < 3)
                return BadRequest("Имя пользователя должно содержать минимум 3 символа");
            
            if (dto.UserName.Length > 50)
                return BadRequest("Имя пользователя не должно превышать 50 символов");

            // 3. Проверка уникальности (исключая текущего пользователя)
            var exists = await _context.Users.AnyAsync(u => u.UserName == dto.UserName && u.UserId != userId);
            if (exists)
            {
                return BadRequest("Пользователь с таким именем уже существует");
            }

            // 4. Поиск и обновление пользователя
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Пользователь не найден");
            }

            user.UserName = dto.UserName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Имя пользователя успешно обновлено", userName = user.UserName });
        }

        // ========================================================================
        // [НАЗНАЧЕНИЕ] Регистрация нового пользователя
        // [ДОСТУП] Разрешено всем
        // ========================================================================
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            user.RegistrationDate = DateTime.UtcNow;
            user.IsActive = true;
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // [БЕЗОПАСНОСТЬ] Очищаем пароль в ответе
            var result = user;
            result.PasswordHash = "";
            
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, result);
        }

        // ========================================================================
        // [НАЗНАЧЕНИЕ] Удаление пользователя
        // [ДОСТУП] Только администраторы
        // ========================================================================
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

    // ============================================================================
    // [НАЗНАЧЕНИЕ] DTO для запроса смены имени пользователя
    // ============================================================================
    public class UpdateUsernameDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
    }
}