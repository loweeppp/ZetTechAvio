using System.ComponentModel.DataAnnotations;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public interface IUserValidationService
    {
        Task<(bool IsValid, string? ErrorMessage)> ValidateRegistrationAsync(string email, string password, string fullName, string phone);
        Task<bool> EmailExistsAsync(string email);
    }

    public class UserValidationService : IUserValidationService
    {
        private readonly ApplicationDbContext _context;

        public UserValidationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateRegistrationAsync(
            string email, string password, string fullName, string phone)
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email is required");

            if (email.Length > 255)
                return (false, "Email is too long");

            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(email))
                return (false, "Email format is invalid");

            // Check if email already exists
            if (await EmailExistsAsync(email))
                return (false, "This email is already registered");

            // Validate password
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required");

            if (password.Length < 6)
                return (false, "Password must be at least 6 characters long");

            if (password.Length > 128)
                return (false, "Password is too long");

            // Validate full name
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name is required");

            if (fullName.Length > 255)
                return (false, "Full name is too long");

            // Validate phone
            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 20)
                return (false, "Phone number is too long");

            return (true, null);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await Task.Run(() => 
                _context.Users.Any(u => u.Email == email.ToLower())
            );
        }
    }
}
