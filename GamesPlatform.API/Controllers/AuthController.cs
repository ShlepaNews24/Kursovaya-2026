// [ФАЙЛ] GamesPlatform.API/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

// ✅ OpenIddict using-директивы (ОБЯЗАТЕЛЬНО)
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

using GamesPlatform.API.Features.Auth;
using GamesPlatform.API.Models;
using Microsoft.AspNetCore;

namespace GamesPlatform.API.Controllers
{
    // 🔐 OAuth2 Token Endpoint — стандартный путь для OpenIddict
    [ApiController]
    [Route("connect/token")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService) => _authService = authService;

        [HttpPost]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest();
            if (request == null)
                return BadRequest(new { error = "invalid_request" });

            // 🔐 Password Grant: login/password → access_token
            if (request.IsPasswordGrantType())
            {
                var user = await _authService.GetUserByEmailAsync(request.Username);
                if (user == null || !await _authService.ValidateCredentialsAsync(request.Username, request.Password))
                    return Unauthorized(new { error = "invalid_grant", error_description = "Invalid credentials" });

                // Формируем Claims для токена
                var identity = new ClaimsIdentity("OpenIddict");
                identity.SetClaim(Claims.Subject, user.UserId.ToString());
                identity.SetClaim(Claims.Name, user.UserName);
                identity.SetClaim(Claims.Email, user.Email);
                identity.SetClaim("user_type", user.UserType);

                // Скоупы и ресурсы
                identity.SetScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email, "games:read", "games:write");
                identity.SetResources("GamesPlatformAPI");

                var principal = new ClaimsPrincipal(identity);
                
                // ✅ OpenIddict сам сгенерирует JWT и вернёт стандартный ответ
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // 🔁 Refresh Token
            if (request.IsRefreshTokenGrantType())
            {
                var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
                if (principal == null)
                    return Unauthorized(new { error = "invalid_grant" });
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return BadRequest(new { error = "unsupported_grant_type" });
        }
    }

    // 📝 Простой контроллер для регистрации (удобно для клиента)
    [ApiController]
    [Route("api/auth")]
    public class RegistrationController : ControllerBase
    {
        private readonly IAuthService _authService;
        public RegistrationController(IAuthService authService) => _authService = authService;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var result = await _authService.RegisterAsync(dto);
                return Ok(new { message = "OK", user = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("info")]
        [Authorize]
        public IActionResult GetUserInfo()
        {
            return Ok(new
            {
                userId = User.FindFirst("user_id")?.Value,
                email = User.FindFirst(Claims.Email)?.Value,
                role = User.FindFirst(Claims.Role)?.Value
            });
        }
    }
}