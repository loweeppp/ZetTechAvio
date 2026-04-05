namespace ZetTechAvio1._0.Controllers
{
    /// <summary>
    /// Безопасный response после login/register
    /// </summary>
    public class LoginResponse
    {
        public string Message { get; set; } = "";
        public string Token { get; set; } = "";
        public int UserId { get; set; }
    }

    /// <summary>
    /// DTO для текущего пользователя из endpoint GET /api/auth/current
    /// Содержит только публичные данные, НЕ содержит passwordHash
    /// </summary>
    public class CurrentUserResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Role { get; set; } = "";
    }

    /// <summary>
    /// Request для регистрации
    /// </summary>
    public class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    /// <summary>
    /// Request для входа
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    /// <summary>
    /// Request для изменения данных пользователя
    /// </summary>
    public class ChangeRequest
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}
