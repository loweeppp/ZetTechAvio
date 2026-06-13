using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ZetTechAvio1._0.Models;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IAuthStateService _authStateService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IDeviceTokenService _deviceTokenService;
        private readonly IConfirmationService _confirmationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthenticationService authService, 
            IAuthStateService authStateService,
            IJwtTokenService jwtTokenService,
            IDeviceTokenService deviceTokenService,
            IConfirmationService confirmationService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _authStateService = authStateService;
            _jwtTokenService = jwtTokenService;
            _deviceTokenService = deviceTokenService;
            _confirmationService = confirmationService;
            _logger = logger;
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, user) = await _authService.RegisterAsync(
                request.Email, request.Password, request.FullName, request.Phone);

            if (!success || user == null)
                return BadRequest(new { message });

            await _authStateService.SetUserAsync(user);
            
            var token = _jwtTokenService.GenerateToken(user);
            return Ok(new LoginResponse
            {
                Message = message,
                Token = token,
                UserId = user.Id
            });
        }

        /// <summary>
        /// Вход в систему
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, user) = await _authService.LoginAsync(request.Email, request.Password);

            if (!success || user == null)
                return BadRequest(new { message });

            if (user.Role == UserRole.Admin)
            {
                var hasActiveDevice = await _deviceTokenService.HasActiveDeviceTokenAsync(user.Id);
                if (hasActiveDevice)
                {
                    return BadRequest(new { message = "Неверный логин или пароль." });
                }
            }

            await _authStateService.SetUserAsync(user);
            
            var token = _jwtTokenService.GenerateToken(user);
            string? deviceToken = null;

            if (user.Role == UserRole.Admin)
            {
                deviceToken = _deviceTokenService.GenerateDeviceToken();
                await _deviceTokenService.StoreDeviceTokenAsync(user.Id, deviceToken, request.Email);
            }

            return Ok(new LoginResponse
            {
                Message = message,
                Token = token,
                UserId = user.Id,
                DeviceToken = deviceToken
            });
        }

        /// <summary>
        /// Получить текущего пользователя (эндпоинт для фронтенда при загрузке)
        /// Требует действительный JWT токен
        /// Возвращает публичные данные пользователя БЕЗ passwordHash
        /// </summary>
        [HttpGet("current")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return Unauthorized();

            return Ok(new CurrentUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Role = user.Role.ToString()
            });
        }

        /// <summary>
        /// Изменить данные пользователя
        /// Требует авторизацию
        /// </summary>
        [HttpPost("change")]
        [Authorize]
        public async Task<IActionResult> Change([FromBody] ChangeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, user) = await _authService.ChangeAsync(
                request.Email, request.Password, request.FullName, request.Phone, request.Id);

            if (!success)
                return BadRequest(new { message });

            await _authStateService.SetUserAsync(user);
            
            var token = _jwtTokenService.GenerateToken(user);
            return Ok(new LoginResponse
            {
                Message = message,
                Token = token,
                UserId = user.Id
            });
        }

        /// <summary>
        /// Выход из системы
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var deviceToken = Request.Headers["X-Device-Token"].FirstOrDefault();
            var logoutPerformed = false;

            _logger.LogInformation("Logout request received. userIdClaim={UserIdClaim} deviceTokenPresent={DeviceTokenPresent}", userIdClaim, !string.IsNullOrWhiteSpace(deviceToken));

            if (!string.IsNullOrWhiteSpace(deviceToken))
            {
                await _deviceTokenService.DeleteDeviceTokensByRawValueAsync(deviceToken);
                logoutPerformed = true;
                _logger.LogInformation("Deleted admin device token by raw deviceToken.");
            }
            else if (int.TryParse(userIdClaim, out var userId))
            {
                await _deviceTokenService.DeleteDeviceTokensAsync(userId);
                logoutPerformed = true;
                _logger.LogInformation("Deleted admin device tokens by userId {UserId}.", userId);
            }



            if (!logoutPerformed)
            {
                _logger.LogWarning("Logout failed: no userId or device token found. userIdClaim={UserIdClaim} deviceTokenPresent={DeviceTokenPresent}", userIdClaim, !string.IsNullOrWhiteSpace(deviceToken));
                return Unauthorized(new { message = "Не удалось определить пользователя или устройство" });
            }

            try
            {
                await _authStateService.ClearUserAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear auth state during logout for userIdClaim={UserIdClaim}", userIdClaim);
            }

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email обязателен" });

            if (!new EmailAddressAttribute().IsValid(request.Email))
                return BadRequest(new { message = "Неверный формат email" });

            var success = await _confirmationService.GenerateCodeAsync(request.Email, Response);

            if (!success)
                return StatusCode(500, new { message = "Не удалось отправить код. Попробуйте позже." });

            return Ok(new { success = true, message = "Код для сброса пароля отправлен на почту." });
        }

        [HttpPost("verify-password-reset-code")]
        public async Task<IActionResult> VerifyPasswordResetCode([FromBody] VerifyCodeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { message = "Email и код обязательны" });

            var isValid = await _confirmationService.VerifyCodeAsync(request.Email, request.Code, Request, Response, deleteOnSuccess: false);
            if (!isValid)
                return BadRequest(new { message = "Неверный или истёкший код" });

            return Ok(new { success = true, message = "Код подтвержден" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { message = "Email, код и новый пароль обязательны" });

            var codeValid = await _confirmationService.VerifyCodeAsync(request.Email, request.Code, Request, Response);
            if (!codeValid)
                return BadRequest(new { message = "Неверный или истёкший код" });

            var (success, message) = await _authService.ResetPasswordAsync(request.Email, request.NewPassword);
            if (!success)
                return BadRequest(new { message });

            return Ok(new { success = true, message });
        }
    }
}
