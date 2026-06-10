using System.ComponentModel.DataAnnotations;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public interface IUserValidationService
    {
        Task<(bool IsValid, string? ErrorMessage)> ValidateRegistrationAsync(string email, string password, string fullName, string phone);
        Task<(bool IsValid, string? ErrorMessage)> ValidateUpdateAsync(string email, string password, string fullName, string phone, int userId);
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
                return (false, "Email обязателен");

            if (email.Length > 255)
                return (false, "Email слишком длинный");

            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(email))
                return (false, "Неверный формат email");

            // Check if email already exists
            if (await EmailExistsAsync(email))
                return (false, "Этот email уже зарегистрирован");

            // Validate password
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Требуется ввести пароль");

            if (password.Length < 6)
                return (false, "Пароль должен содержать не менее 6 символов");

            if (password.Length > 128)
                return (false, "Пароль слишком длинный");

            // Validate full name
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Имя  обязательно");

            if (fullName.Length > 255)
                return (false, "Имя  слишком длинное");

            // Validate phone
            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 20)
                return (false, "Номер телефона слишком длинный");

            return (true, null);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await Task.Run(() => 
                _context.Users.Any(u => u.Email == email.ToLower())
            );
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateUpdateAsync(
            string email, string password, string fullName, string phone, int userId)
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email обязателен");

            if (email.Length > 255)
                return (false, "Email слишком длинный");

            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(email))
                return (false, "Неверный формат email");

            // Check if email already exists (but exclude current user)
            var emailExists = await Task.Run(() => 
                _context.Users.Any(u => u.Email == email.ToLower() && u.Id != userId)
            );
            if (emailExists)
                return (false, "Этот email уже зарегистрирован");

            // Validate password (optional for update)
            if (!string.IsNullOrWhiteSpace(password))
            {
                if (password.Length < 6)
                    return (false, "Пароль должен содержать не менее 6 символов");

                if (password.Length > 128)
                    return (false, "Пароль слишком длинный");
            }

            // Validate full name
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Имя  обязательно");

            if (fullName.Length > 255)
                return (false, "Имя  слишком длинное");

            // Validate phone
            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 20)
                return (false, "Номер телефона слишком длинный");

            return (true, null);
        }
    }
}
