using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ZetTechAvio1._0.Services
{
    public record EmailAttachment(string Type, string Name, string Content, string? ContentId = null);

    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, IEnumerable<EmailAttachment>? attachments = null);
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

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, IEnumerable<EmailAttachment>? attachments = null)
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
                return await SendWithResendAsync(to, subject, body, isHtml, attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки email через Resend");
                return false;
            }
        }

        private async Task<bool> SendWithResendAsync(string to, string subject, string body, bool isHtml, IEnumerable<EmailAttachment>? attachments)
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
                text = isHtml ? ConvertHtmlToPlainText(body) : body,
                attachments = attachments?.Select(a => new { type = a.Type, name = a.Name, content = a.Content, content_id = a.ContentId }).ToArray()
            };

            var serializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, serializerOptions), Encoding.UTF8, "application/json")
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

        private static string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var text = Regex.Replace(html, "<style[^>]*>.*?</style>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<script[^>]*>.*?</script>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", string.Empty);
            text = WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }

        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && email.Contains("@");
        }
    }
}
