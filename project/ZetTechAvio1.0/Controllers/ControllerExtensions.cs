using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ZetTechAvio1._0.Controllers
{
    /// <summary>
    /// Extension methods for ControllerBase to extract user info from JWT token
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// Extract userId from JWT token claims
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <param name="userId">Output userId (0 if not found)</param>
        /// <returns>true if userId extracted successfully, false if unauthorized</returns>
        public static bool TryGetUserId(this ControllerBase controller, out int userId)
        {
            userId = 0;
            var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
            {
                return false;
            }

            return true;
        }
    }
}
