using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public interface IDeviceTokenService
    {
        string GenerateDeviceToken();
        string HashDeviceToken(string token);
        Task<bool> ValidateDeviceTokenAsync(int adminUserId, string token);
        Task<bool> HasActiveDeviceTokenAsync(int adminUserId);
        Task StoreDeviceTokenAsync(int adminUserId, string token, string? deviceInfo = null);
        Task DeleteDeviceTokensAsync(int adminUserId, string? token = null);
        Task DeleteDeviceTokensByRawValueAsync(string token);
    }

    public class DeviceTokenService : IDeviceTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeviceTokenService> _logger;

        public DeviceTokenService(ApplicationDbContext context, ILogger<DeviceTokenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string GenerateDeviceToken()
        {
            return Guid.NewGuid().ToString();
        }

        public string HashDeviceToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        public async Task StoreDeviceTokenAsync(int adminUserId, string token, string? deviceInfo = null)
        {
            var hash = HashDeviceToken(token);
            _logger.LogInformation("Storing admin device token for user {AdminUserId}; deviceInfo={DeviceInfo}", adminUserId, deviceInfo);

            var device = new AdminDevice
            {
                AdminUserId = adminUserId,
                DeviceTokenHash = hash,
                DeviceInfo = deviceInfo,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };

            _context.AdminDevices.Add(device);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasActiveDeviceTokenAsync(int adminUserId)
        {
            return await _context.AdminDevices.AnyAsync(d => d.AdminUserId == adminUserId);
        }

        public async Task DeleteDeviceTokensAsync(int adminUserId, string? token = null)
        {
            IQueryable<AdminDevice> query = _context.AdminDevices.Where(d => d.AdminUserId == adminUserId);
            if (!string.IsNullOrWhiteSpace(token))
            {
                var hash = HashDeviceToken(token);
                query = query.Where(d => d.DeviceTokenHash == hash);
                _logger.LogInformation("Deleting admin device tokens for user {AdminUserId} by token hash.", adminUserId);
            }
            else
            {
                _logger.LogInformation("Deleting all admin device tokens for user {AdminUserId}.", adminUserId);
            }

            var devices = await query.ToListAsync();
            _logger.LogInformation("Found {Count} admin device token(s) to delete for user {AdminUserId}.", devices.Count, adminUserId);
            _context.AdminDevices.RemoveRange(devices);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDeviceTokensByRawValueAsync(string token)
        {
            var hash = HashDeviceToken(token);
            _logger.LogInformation("Deleting admin device tokens by device token raw value; hash={Hash}.", hash);
            var devices = await _context.AdminDevices
                .Where(d => d.DeviceTokenHash == hash)
                .ToListAsync();

            _logger.LogInformation("Found {Count} admin device token(s) to delete by raw token.", devices.Count);
            _context.AdminDevices.RemoveRange(devices);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ValidateDeviceTokenAsync(int adminUserId, string token)
        {
            var hash = HashDeviceToken(token);
            var device = await _context.AdminDevices
                .Where(d => d.AdminUserId == adminUserId && d.DeviceTokenHash == hash)
                .FirstOrDefaultAsync();

            if (device == null)
            {
                _logger.LogWarning("Invalid admin device token for user {AdminUserId}; hash={Hash}.", adminUserId, hash);
                return false;
            }

            device.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
