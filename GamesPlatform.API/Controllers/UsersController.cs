using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using GamesPlatform.API.Models;
using Microsoft.AspNetCore.Authorization;

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

        // Получит список всех пользователей
        // Доступ у админов
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

        [HttpGet("{id}")]
        [Authorize]  // ← Просмотр своего профиля
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Не возвращаем пароль
            user.PasswordHash = "";

            return user;
        }

        [HttpPut("{id}")]
        [Authorize]  // ← Редактирование — автор или админ
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
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

        [HttpPost]
        [AllowAnonymous]  // ← Регистрация 
        public async Task<ActionResult<User>> PostUser(User user)
        {
            user.RegistrationDate = DateTime.UtcNow;
            user.IsActive = true;
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]  // ← Удаление аккаунта - только админ
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
}