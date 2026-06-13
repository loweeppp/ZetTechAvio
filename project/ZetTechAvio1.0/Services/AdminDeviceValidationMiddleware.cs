using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Services
{
    public class AdminDeviceValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminDeviceValidationMiddleware> _logger;

        public AdminDeviceValidationMiddleware(RequestDelegate next, ILogger<AdminDeviceValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IDeviceTokenService deviceTokenService)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith("/api/manager", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Не авторизован" });
                return;
            }

            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrWhiteSpace(roleClaim) || !(roleClaim == "Admin" || roleClaim == "Manager"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Нет доступа" });
                return;
            }

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Не удалось определить пользователя" });
                return;
            }

            if (roleClaim == "Admin")
            {
                var deviceToken = context.Request.Headers["X-Device-Token"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(deviceToken))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { success = false, message = "Device token required" });
                    return;
                }

                var valid = await deviceTokenService.ValidateDeviceTokenAsync(userId, deviceToken);
                if (!valid)
                {
                    _logger.LogWarning("Invalid device token for user {UserId} on path {Path}", userId, path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { success = false, message = "Invalid device token" });
                    return;
                }
            }

            await _next(context);
        }
    }
}
