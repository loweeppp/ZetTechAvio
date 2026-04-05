using ZetTechAvio1._0.Components.Pages;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;
using Microsoft.EntityFrameworkCore;

namespace ZetTechAvio1._0.Services
{
    public interface IAuthenticationService
    {
        Task<(bool Success, string? Message, User? User)> RegisterAsync(string email, string password, string fullName, string phone);
        Task<(bool Success, string? Message, User? User)> LoginAsync(string email, string password);
        Task<(bool Success, string? Message, User? User)> ChangeAsync(string email, string password, string fullName, string phone, int Id);
        Task<User?> GetUserByIdAsync(int userId);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHashingService _passwordService;
        private readonly IUserValidationService _validationService;

        public AuthenticationService(
            ApplicationDbContext context,
            IPasswordHashingService passwordService,
            IUserValidationService validationService)
        {
            _context = context;
            _passwordService = passwordService;
            _validationService = validationService;
        }

        public async Task<(bool Success, string? Message, User? User)> ChangeAsync(
            string email, string password, string fullName, string phone, int Id)
        {
            try
            {
                //Validate input
                var (isValid, errorMessage) = await _validationService.ValidateUpdateAsync(
                    email, password, fullName, phone, Id);

                if (!isValid)
                    return (false, errorMessage, null);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
    
                if (user == null)
                    return (false, "User not found", null);

                var passwordHash = _passwordService.HashPassword(password);

                // Update user details
                user.FullName = fullName;
                user.Phone = phone;
                user.PasswordHash = passwordHash;
                user.Email = email.ToLower();

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(password))
                {
                    user.PasswordHash = _passwordService.HashPassword(password);
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return (true, "User updated successfully", user);
            }
            catch (Exception ex)
            {
                return (false, $"Update error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string? Message, User? User)> RegisterAsync(
            string email, string password, string fullName, string phone)
        {
            try
            {
                // Validate input
                var (isValid, errorMessage) = await _validationService.ValidateRegistrationAsync(
                    email, password, fullName, phone);

                if (!isValid)
                    return (false, errorMessage, null);

                // Hash password
                var passwordHash = _passwordService.HashPassword(password);

                // Create new user
                var user = new User
                {
                    Email = email.ToLower(),
                    PasswordHash = passwordHash,
                    FullName = fullName,
                    Phone = phone,
                    Role = UserRole.User,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add to database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return (true, "Registration successful", user);
            }
            catch (Exception ex)
            {
                return (false, $"Registration error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string? Message, User? User)> LoginAsync(
            string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return (false, "Email and password are required", null);

                // Find user by email
                var user = await Task.Run(() =>
                    _context.Users.FirstOrDefault(u => u.Email == email.ToLower())
                );

                if (user == null)
                {
                    Console.WriteLine($"[LOGIN] User not found: {email}");
                    return (false, "Invalid email or password", null);
                }

                // Console.WriteLine($"[LOGIN] User found: {user.Email}");
                // Console.WriteLine($"[LOGIN] PasswordHash exists: {!string.IsNullOrEmpty(user.PasswordHash)}");

                // Verify password
                bool passwordValid = _passwordService.VerifyPassword(password, user.PasswordHash);
                // Console.WriteLine($"[LOGIN] Password verification result: {passwordValid}");

                if (!passwordValid)
                    return (false, "Invalid email or password", null);

                Console.WriteLine($"[LOGIN] Login successful!");
                return (true, "Login successful", user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] Exception: {ex.Message}\n{ex.StackTrace}");
                return (false, $"Login error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Получить пользователя по ID
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetUserById] Error: {ex.Message}");
                return null;
            }
        }
    }
}
