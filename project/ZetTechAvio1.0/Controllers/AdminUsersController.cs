using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin,Manager")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserManagementService _managementService;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            IUserManagementService managementService,
            ILogger<AdminUsersController> logger)
        {
            _managementService = managementService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var (success, message, data, total) = await _managementService.GetUsersAsync(page, pageSize, search);
            if (!success)
                return StatusCode(500, new { success, message });

            return Ok(new { success, message, data, total });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var (success, message, data) = await _managementService.GetUserByIdAsync(id);
            if (!success)
                return NotFound(new { success, message });

            return Ok(new { success, message, data });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUserUpdateRequest request)
        {
            var (success, message) = await _managementService.UpdateUserAsync(id, request);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }

        [HttpPost("{id}/toggle-block")]
        public async Task<IActionResult> ToggleBlock(int id)
        {
            var (success, message) = await _managementService.ToggleUserActiveAsync(id);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
    }

    public sealed record ToggleBlockRequest(bool IsBlocked);
}
