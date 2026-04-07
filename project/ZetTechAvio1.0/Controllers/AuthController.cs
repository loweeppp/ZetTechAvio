using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

        public AuthController(
            IAuthenticationService authService, 
            IAuthStateService authStateService,
            IJwtTokenService jwtTokenService)
        {
            _authService = authService;
            _authStateService = authStateService;
            _jwtTokenService = jwtTokenService;
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
        /// Получить текущего пользователя (эндпоинт для фронтенда при загрузке)
        /// Требует действительный JWT токен
        /// Возвращает публичные данные пользователя БЕЗ passwordHash
        /// </summary>
        [HttpGet("current")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
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
            await _authStateService.ClearUserAsync();
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
