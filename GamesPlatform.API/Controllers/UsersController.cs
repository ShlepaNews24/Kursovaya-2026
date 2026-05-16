using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models;
using GamesPlatform.API.Services;

namespace GamesPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IFileService _fileService;

        public UsersController(IUnitOfWork uow, IFileService fileService)
        {
            _uow = uow;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _uow.Users.GetAllAsync();
            var result = users.Select(u => new UserDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                Email = u.Email,
                UserType = u.UserType,
                IsActive = u.IsActive,
                RegistrationDate = u.RegistrationDate,
                LastLoginDate = u.LastLoginDate,
                DateOfBirth = u.DateOfBirth,
                AvatarUrl = u.AvatarUrl
            }).ToList();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(new UserDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                UserType = user.UserType,
                IsActive = user.IsActive,
                RegistrationDate = user.RegistrationDate,
                LastLoginDate = user.LastLoginDate,
                DateOfBirth = user.DateOfBirth,
                AvatarUrl = user.AvatarUrl
            });
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserType))
                return BadRequest("Role cannot be empty");

            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) return NotFound("User not found");

            user.UserType = dto.UserType;
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveAsync();

            return Ok(new { message = "Role updated", userName = user.UserName, userType = user.UserType });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) return NotFound("User not found");

            user.IsActive = dto.IsActive;
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveAsync();

            return Ok(new { message = dto.IsActive ? "User unblocked" : "User blocked", isActive = user.IsActive });
        }

        [HttpPut("me/username")]
        [Authorize]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest("Username cannot be empty");

            dto.UserName = dto.UserName.Trim();
            if (dto.UserName.Length < 3 || dto.UserName.Length > 50)
                return BadRequest("Username must be 3-50 characters");

            var existingUsers = await _uow.Users.FindAsync(u => u.UserName == dto.UserName && u.UserId != userId);
            if (existingUsers.Any()) return BadRequest("Username already taken");

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return NotFound();

            user.UserName = dto.UserName;
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveAsync();

            return Ok(new { message = "Username updated", userName = user.UserName });
        }

        [HttpPut("me/dateofbirth")]
        [Authorize]
        public async Task<IActionResult> UpdateDateOfBirth([FromBody] UpdateDateOfBirthDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value > DateTime.UtcNow)
                return BadRequest("Date of birth cannot be in the future");

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return NotFound();

            user.DateOfBirth = dto.DateOfBirth;
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveAsync();

            return Ok(new { message = "Date of birth updated", dateOfBirth = user.DateOfBirth });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) return NotFound();
            await _uow.Users.DeleteAsync(user);
            await _uow.SaveAsync();
            return NoContent();
        }

        [HttpGet("me/avatar")]
        [Authorize]
        public async Task<ActionResult<object>> GetMyAvatar()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(new { avatarUrl = user.AvatarUrl });
        }

        [HttpPost("me/avatar")]
        [Authorize]
        public async Task<ActionResult<object>> UploadAvatar(IFormFile file)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return NotFound();

            if (!_fileService.IsValidFile(file, out var error))
                return BadRequest(new { error });

            var fileUrl = await _fileService.UploadFileAsync(file, "avatars");
            if (fileUrl == null)
                return StatusCode(500, new { error = "Failed to upload avatar" });

            if (!string.IsNullOrEmpty(user.AvatarUrl))
                await _fileService.DeleteFileAsync(user.AvatarUrl);

            user.AvatarUrl = fileUrl;
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveAsync();

            return Ok(new { avatarUrl = fileUrl });
        }
        
        [HttpDelete("me/avatar")]
        [Authorize]
        public async Task<IActionResult> DeleteAvatar()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(user.AvatarUrl))
                await _fileService.DeleteFileAsync(user.AvatarUrl);

            user.AvatarUrl = null;
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveAsync();

            return NoContent();
        }
    }

    public class UpdateUsernameDto { [Required] public string UserName { get; set; } = string.Empty; }
    public class UpdateDateOfBirthDto { public DateTime? DateOfBirth { get; set; } }
    public class UpdateRoleDto { [Required] public string UserType { get; set; } = string.Empty; }
    public class UpdateStatusDto { public bool IsActive { get; set; } }
    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
    }
}