using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public interface IAuthenticationService
    {
        Task<(bool Success, string? Message, User? User)> RegisterAsync(string email, string password, string fullName, string phone);
        Task<(bool Success, string? Message, User? User)> LoginAsync(string email, string password);
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

        public async Task<(bool Success, string? Message, User? User)> LoginAsync(string email, string password)
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
                    return (false, "Invalid email or password", null);

                // Verify password
                if (!_passwordService.VerifyPassword(password, user.PasswordHash))
                    return (false, "Invalid email or password", null);

                return (true, "Login successful", user);
            }
            catch (Exception ex)
            {
                return (false, $"Login error: {ex.Message}", null);
            }
        }
    }
}
