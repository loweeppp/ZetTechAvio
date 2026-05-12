using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ZetTechAvio1._0.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    }

    public class EmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(HttpClient httpClient, IConfiguration config, ILogger<EmailService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            var smtpFromEmail = _config["SmtpSettings:Email"] ?? _config["SMTP_USER"] ?? string.Empty;
            _apiKey = _config["EmailApi:ApiKey"] ?? _config["RESEND_API_KEY"] ?? string.Empty;
            _fromEmail = _config["EmailApi:FromEmail"] ?? _config["EMAIL_FROM_EMAIL"] ?? smtpFromEmail ?? string.Empty;
            _fromName = _config["EmailApi:FromName"] ?? _config["EMAIL_FROM_NAME"] ?? "ZetTechAvio";

            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_fromEmail))
            {
                _logger.LogWarning("Resend Email API не настроен полностью: ApiKeySet={ApiKeySet}, FromEmail={FromEmail}", !string.IsNullOrWhiteSpace(_apiKey), _fromEmail);
            }
            else
            {
                _logger.LogInformation("Resend Email API настроен");
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Resend API ключ не задан. Отправка письма пропущена.");
                return false;
            }

            if (!IsValidEmail(to))
            {
                _logger.LogWarning("Неверный адрес получателя: {Email}", to);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_fromEmail))
            {
                _logger.LogWarning("Email API from.email не настроен.");
                return false;
            }

            try
            {
                return await SendWithResendAsync(to, subject, body, isHtml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки email через Resend");
                return false;
            }
        }

        private async Task<bool> SendWithResendAsync(string to, string subject, string body, bool isHtml)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Resend API ключ не задан.");
                return false;
            }

            var payload = new
            {
                from = _fromEmail,
                to = new[] { to },
                subject,
                html = isHtml ? body : null,
                text = isHtml ? null : body
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Письмо отправлено через Resend на {Email}", to);
                return true;
            }

            var errorText = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Resend отправка не удалась: {StatusCode} {Body}", response.StatusCode, errorText);
            return false;
        }

        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && email.Contains("@");
        }
    }
}
