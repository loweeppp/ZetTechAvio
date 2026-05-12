using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public interface IUserManagementService
    {
        Task<(bool Success, string Message, List<AdminUserDto>? Data, int TotalCount)> GetUsersAsync(int page = 1, int pageSize = 20, string? search = null);
        Task<(bool Success, string Message, AdminUserDto? Data)> GetUserByIdAsync(int id);
        Task<(bool Success, string Message)> UpdateUserAsync(int id, AdminUserUpdateRequest request);
        Task<(bool Success, string Message)> ToggleUserActiveAsync(int id, bool? isActive = null);
    }

    public sealed record AdminUserDto(
        int Id,
        string Email,
        string FullName,
        string Phone,
        string Role,
        bool IsActive,
        DateTime CreatedAt
    );

    public sealed record AdminUserUpdateRequest(
        string Email,
        string FullName,
        string Phone,
        string? Password,
        string? Role
    );

    public class UserManagementService : IUserManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHashingService _hashService;
        private readonly IUserValidationService _validationService;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            ApplicationDbContext context,
            IPasswordHashingService hashService,
            IUserValidationService validationService,
            ILogger<UserManagementService> logger)
        {
            _context = context;
            _hashService = hashService;
            _validationService = validationService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, List<AdminUserDto>? Data, int TotalCount)> GetUsersAsync(int page = 1, int pageSize = 20, string? search = null)
        {
            try
            {
                var query = _context.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var normalizedSearch = search.Trim().ToLower();
                    query = query.Where(u =>
                        u.FullName.ToLower().Contains(normalizedSearch) ||
                        u.Email.ToLower().Contains(normalizedSearch) ||
                        u.Phone.ToLower().Contains(normalizedSearch)
                    );
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = users.Select(u => new AdminUserDto(
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.Phone,
                    u.Role.ToString(),
                    u.IsActive,
                    u.CreatedAt
                )).ToList();

                return (true, "OK", result, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin user list");
                return (false, "Не удалось получить список пользователей", null, 0);
            }
        }

        public async Task<(bool Success, string Message, AdminUserDto? Data)> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return (false, "Пользователь не найден", null);

                var dto = new AdminUserDto(
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Phone,
                    user.Role.ToString(),
                    user.IsActive,
                    user.CreatedAt
                );

                return (true, "OK", dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin user by id");
                return (false, "Ошибка получения данных", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(int id, AdminUserUpdateRequest request)
        {
            try
            {
                var (isValid, errorMessage) = await _validationService.ValidateUpdateAsync(
                    request.Email,
                    request.Password ?? string.Empty,
                    request.FullName,
                    request.Phone,
                    id);

                if (!isValid)
                    return (false, errorMessage ?? "Некорректные данные");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return (false, "Пользователь не найден");

                user.Email = request.Email.ToLower().Trim();
                user.FullName = request.FullName.Trim();
                user.Phone = request.Phone.Trim();

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    user.PasswordHash = _hashService.HashPassword(request.Password);
                }

                if (!string.IsNullOrWhiteSpace(request.Role) &&
                    Enum.TryParse<UserRole>(request.Role, true, out var parsedRole))
                {
                    user.Role = parsedRole;
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return (true, "Данные пользователя обновлены");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin user");
                return (false, "Ошибка обновления пользователя");
            }
        }

        public async Task<(bool Success, string Message)> ToggleUserActiveAsync(int id, bool? isActive = null)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return (false, "Пользователь не найден");

                var newState = isActive ?? !user.IsActive;
                user.IsActive = newState;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return (true, newState ? "Пользователь активирован" : "Пользователь заблокирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user active state");
                return (false, "Ошибка смены статуса пользователя");
            }
        }
    }
}
