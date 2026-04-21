using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net;

namespace ZetTechAvio1._0.Services
{
    public interface IPaymentService
    {
        Task<Payment?> CreatePaymentAsync(int bookingId, string description);
        Task<Payment?> VerifyAndUpdatePaymentStatusAsync(int bookingId, string yooKassaPaymentId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _shopId;
        private readonly string _apiKey;

        public PaymentService(ApplicationDbContext context, IConfiguration config, ILogger<PaymentService> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _httpClient = httpClient;

            _shopId = _config["YOOKASSA_SHOP_ID"] ?? "";
            _apiKey = _config["YOOKASSA_API_KEY"] ?? "";

            if (string.IsNullOrWhiteSpace(_shopId) || string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("YooKassa не настроена: YOOKASSA_SHOP_ID или YOOKASSA_API_KEY не установлены");
            }
            else
            {
                _logger.LogInformation($"YooKassa инициализирована. ShopID: {_shopId}");
            }
        }

        public async Task<Payment?> CreatePaymentAsync(int bookingId, string description)
        {
            try
            {
                // Проверка что YooKassa настроена
                if (string.IsNullOrWhiteSpace(_shopId) || string.IsNullOrWhiteSpace(_apiKey))
                {
                    _logger.LogError("YooKassa не настроена. Платеж не может быть создан.");
                    return null;
                }

                // 1. Получить бронирование
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Бронирование с ID {BookingId} не найдено", bookingId);
                    return null;
                }

                // 2. Создать платеж в YooKassa через REST API 
                
                var requestBody = new
                {
                    amount = new
                    {
                        value = booking.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                        currency = "RUB"
                    },
                    confirmation = new
                    {
                        type = "redirect",
                        return_url = _config["YOOKASSA_RETURN_URL"] ?? "https://zettechavio.ru/bookings"
                    },
                    capture = true,
                    description = description,
                    notification_url = "https://api.zettechavio.ru/api/payment/webhook",
                    metadata = new
                    {
                        booking_id = bookingId,
                        booking_reference = booking.BookingReference
                    }
                };

                _logger.LogInformation($" YooKassa запрос с notification_url: https://api.zettechavio.ru/api/payment/webhook");

                var jsonPayload = JsonConvert.SerializeObject(requestBody);
                _logger.LogInformation($"[PAYMENT DEBUG] JSON Request Payload:\n{jsonPayload}");
                
                var jsonContent = new StringContent(
                    jsonPayload,
                    Encoding.UTF8,
                    "application/json"
                );

                // Базовая авторизация для YooKassa API
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_shopId}:{_apiKey}"));
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.yookassa.ru/v3/payments"))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                    requestMessage.Content = jsonContent;
                    requestMessage.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString());

                    var response = await _httpClient.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"YooKassa API ошибка: {response.StatusCode} - {errorContent}");
                        return null;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($" YooKassa API успех: {responseContent}");
                    dynamic result = JsonConvert.DeserializeObject(responseContent);

                    // 3. Сохранить платеж в БД
                    var payment = new Payment
                    {
                        BookingId = bookingId,
                        YooKassaPaymentId = result["id"],
                        TotalAmount = booking.TotalAmount,
                        Status = Payment.PaymentStatus.Pending,
                        ConfirmationUrl = result["confirmation"]["confirmation_url"]?.ToString() ?? result["confirmation"]["return_url"]?.ToString(),
                        Discription = description,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Платеж {result["id"]} создан для бронирования {bookingId}");

                    return payment;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при создании платежа: {ex.Message}");
                return null;
            }
        }



        /// <summary>
        /// Отправляет письмо с подтверждением платежа и QR-кодом билета
        /// </summary>
        private async Task SendPaymentConfirmationEmailAsync(Booking booking, Payment payment)
        {
            try
            {
                _logger.LogInformation($"[PAYMENT_EMAIL] Начало отправки письма для бронирования {booking.Id}");
                
                var userEmail = booking.User?.Email;
                _logger.LogInformation($"[PAYMENT_EMAIL] Email пользователя: {userEmail ?? "ПУСТО"}");
                
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    _logger.LogWarning("Email пользователя не найден для бронирования {BookingId}", booking.Id);
                    return;
                }

                var smtpHost = _config["SmtpSettings:Host"] ?? _config["SMTP_HOST"];
                var smtpPortStr = _config["SmtpSettings:Port"] ?? _config["SMTP_PORT"] ?? "587";
                var senderEmail = _config["SmtpSettings:Email"] ?? _config["SMTP_USER"];
                var senderPassword = _config["SmtpSettings:Password"] ?? _config["SMTP_PASSWORD"];

                _logger.LogInformation($"[PAYMENT_EMAIL] SMTP Config - Host: {smtpHost}, Port: {smtpPortStr}, From: {senderEmail ?? "ПУСТО"}");

                if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(senderEmail))
                {
                    _logger.LogWarning("[PAYMENT_EMAIL] SMTP не настроен, письмо не отправлено");
                    return;
                }

                if (!int.TryParse(smtpPortStr, out int smtpPort))
                    smtpPort = 587;

                using (var smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtp.EnableSsl = smtpPort == 587 || smtpPort == 465;
                    
                    _logger.LogInformation($"[PAYMENT_EMAIL] SMTP клиент создан - EnableSSL: {smtp.EnableSsl}");

                    // Генерируем QR-код (заглушка)
                    var qrCodeData = GenerateQRCodeAsText(booking.BookingReference);

                    var mail = new MailMessage
                    {
                        From = new MailAddress(senderEmail),
                        Subject = $"Подтверждение платежа - Бронирование {booking.BookingReference}",
                        IsBodyHtml = true
                    };

                    // HTML письмо с информацией о платеже и "QR-кодом"
                    var emailBody = $@"
                    <html>
                    <head><meta charset='utf-8'></head>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #2196F3;'>✅ Платеж успешно принят!</h2>
                            
                            <div style='background: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p><strong>Номер бронирования:</strong> {booking.BookingReference}</p>
                                <p><strong>Сумма платежа:</strong> {payment.TotalAmount:F2} ₽</p>
                                <p><strong>Статус:</strong> Подтверждено</p>
                                <p><strong>Дата/время платежа:</strong> {payment.UpdatedAt:dd.MM.yyyy HH:mm:ss}</p>
                            </div>

                            <h3 style='color: #333;'> Ваш QR-код билета:</h3>
                            <div style='background: #fff; padding: 15px; border: 1px solid #ddd; border-radius: 5px; margin: 20px 0; font-family: monospace; font-size: 10px; line-height: 1.2; white-space: pre;'>
{qrCodeData}
                            </div>

                            <p style='color: #666; font-size: 12px;'>
                                 QR-код содержит информацию о вашем бронировании. Покажите его при регистрации в аэропорту.
                            </p>

                            <div style='margin-top: 30px; padding-top: 15px; border-top: 1px solid #ddd;'>
                                <p style='color: #999; font-size: 12px;'>
                                    Письмо отправлено автоматически. Пожалуйста, не отвечайте на это письмо.
                                </p>
                                <p style='color: #999; font-size: 12px;'>
                                    При возникновении вопросов обратитесь в служу поддержки: ZetTechAvioBot@mail.ru
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>
                    ";

                    mail.Body = emailBody;
                    mail.To.Add(userEmail);

                    _logger.LogInformation($"[PAYMENT_EMAIL] Отправка письма на {userEmail}");
                    await smtp.SendMailAsync(mail);
                    _logger.LogInformation($"[PAYMENT_EMAIL] Письмо успешно отправлено на {userEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PAYMENT_EMAIL] ❌ ОШИБКА при отправке письма подтверждения платежа: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                // Не выбрасываем исключение - платеж уже принят
            }
        }

        /// <summary>
        /// Генерирует простой QR-код в виде текста (заглушка)
        /// В продакшене можно использовать библиотеку QRCoder для генерирования реального QR-кода
        /// </summary>
        private string GenerateQRCodeAsText(string bookingReference)
        {
            // Простая текстовая заглушка QR-кода
            // В реальном приложении здесь был бы реальный QR-код
            var qrData = $"BOOKING:{bookingReference}|AIRLINE:ZetTechAvio|TIME:{DateTime.UtcNow:yyyy-MM-dd}";
            
            // Генерируем простой паттерн в стиле ASCII QR-кода
            var ascii = @"
█████████████████████████████████████
█                                   █
█  ██████  ██  ██  ██████  ██████   █
█  ██      ██████  ██  ██  ██  ██   █
█  ██████  ██  ██  ██████  ██████   █
█           " + bookingReference.PadRight(14) + @" █
█                                   █
█████████████████████████████████████
            ";
            
            return ascii;
        }

        /// <summary>
        /// Проверяет статус платежа в YooKassa и обновляет его в БД
        /// Используется для тестирования на localhost (веб-хуки не могут достичь localhost)
        /// </summary>
        public async Task<Payment?> VerifyAndUpdatePaymentStatusAsync(int bookingId, string yooKassaPaymentId)
        {
            try
            {
                // Проверка что YooKassa настроена
                if (string.IsNullOrWhiteSpace(_shopId) || string.IsNullOrWhiteSpace(_apiKey))
                {
                    _logger.LogError("[PAYMENT_VERIFY] YooKassa не настроена");
                    return null;
                }

                _logger.LogInformation($"[PAYMENT_VERIFY] Проверка статуса платежа {yooKassaPaymentId}");

                // Запрашиваем статус платежа из YooKassa
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_shopId}:{_apiKey}"));
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.yookassa.ru/v3/payments/{yooKassaPaymentId}"))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

                    var response = await _httpClient.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"[PAYMENT_VERIFY] Ошибка при получении статуса: {response.StatusCode} - {errorContent}");
                        return null;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseContent);

                    string status = result["status"];
                    bool paid = result["paid"] ?? false;

                    _logger.LogInformation($"[PAYMENT_VERIFY] Статус платежа: {status}, Оплачено: {paid}");

                    // Находим платеж в БД
                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.YooKassaPaymentId == yooKassaPaymentId);

                    if (payment == null)
                    {
                        _logger.LogWarning($"[PAYMENT_VERIFY] Платеж {yooKassaPaymentId} не найден в БД");
                        return null;
                    }

                    // Если уже обработан - не обновляем повторно
                    if (payment.Status != Payment.PaymentStatus.Pending)
                    {
                        _logger.LogInformation($"[PAYMENT_VERIFY] Платеж {yooKassaPaymentId} уже обработан со статусом {payment.Status}");
                        return payment;
                    }

                    // Обновляем статус если платёж успешен
                    if (status == "succeeded" && paid)
                    {
                        _logger.LogInformation($"[PAYMENT_VERIFY] Платеж {yooKassaPaymentId} успешно подтверждён!");

                        payment.Status = Payment.PaymentStatus.Succeeded;
                        payment.UpdatedAt = DateTime.UtcNow;

                        // Обновляем бронирование
                        var booking = await _context.Bookings
                            .Include(b => b.User)
                            .FirstOrDefaultAsync(b => b.Id == bookingId);

                        if (booking != null)
                        {
                            booking.Status = BookingStatus.Confirmed;
                            booking.UpdatedAt = DateTime.UtcNow;

                            _logger.LogInformation($"[PAYMENT_VERIFY] Бронирование {bookingId} обновлено на Confirmed");

                            // Отправляем письмо с подтверждением
                            await SendPaymentConfirmationEmailAsync(booking, payment);
                        }
                        else
                        {
                            _logger.LogWarning($"[PAYMENT_VERIFY] Бронирование {bookingId} не найдено");
                        }

                        await _context.SaveChangesAsync();
                    }
                    else if (status == "failed" || status == "canceled")
                    {
                        _logger.LogWarning($"[PAYMENT_VERIFY] Платеж {yooKassaPaymentId} отклонён: {status}");
                        
                        payment.Status = Payment.PaymentStatus.Failed;
                        payment.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogInformation($"[PAYMENT_VERIFY] Платеж {yooKassaPaymentId} всё ещё в статусе {status}");
                    }

                    return payment;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PAYMENT_VERIFY] Ошибка при проверке статуса: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}

