using Microsoft.AspNetCore.Mvc;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IAuthStateService _authStateService;

        public AuthController(IAuthenticationService authService, IAuthStateService authStateService)
        {
            _authService = authService;
            _authStateService = authStateService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, user) = await _authService.RegisterAsync(
                request.Email, request.Password, request.FullName, request.Phone);

            if (!success)
                return BadRequest(new { message });

            await _authStateService.SetUserAsync(user);
            return Ok(new { message, user });
        }

        [HttpPost("change")]
        public async Task<IActionResult> Change([FromBody] ChangeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, user) = await _authService.ChangeAsync(
                request.Email, request.Password, request.FullName, request.Phone, request.Id);

            if (!success)
                return BadRequest(new { message });

            await _authStateService.SetUserAsync(user);
            return Ok(new { message, user });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, user) = await _authService.LoginAsync(request.Email, request.Password);

            if (!success)
                return BadRequest(new { message });

            await _authStateService.SetUserAsync(user);
            return Ok(new { message, user });
        }



        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authStateService.ClearUserAsync();
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("current")]
        public IActionResult GetCurrentUser()
        {
            var user = _authStateService.CurrentUser;
            if (user == null)
                return Unauthorized();

            return Ok(user);
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class ChangeRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public int Id { get; set; }
    }
}
