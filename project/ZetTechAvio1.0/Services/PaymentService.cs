using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net.Http.Headers;

namespace ZetTechAvio1._0.Services
{
    public interface IPaymentService
    {
        Task<Payment?> CreatePaymentAsync(int bookingId, string description);
        Task<bool> HandleWebhookAsync(string jsonData);
        Task<Payment?> GetPaymentAsync(int paymentId);
    }
    // Реализуйте методы для создания платежа, получения статуса и обработки уведомлений от Яндекс.Кассы

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

            _shopId = _config["YOOKASSA_SHOP_ID"] ?? throw new Exception("YOOKASSA_SHOP_ID не установлен");
            _apiKey = _config["YOOKASSA_API_KEY"] ?? throw new Exception("YOOKASSA_API_KEY не установлен");
        }

        public async Task<Payment?> CreatePaymentAsync(int bookingId, string description)
        {
            try
            {
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
                        value = booking.TotalAmount.ToString("F2"),
                        currency = "RUB"
                    },
                    confirmation = new
                    {
                        type = "redirect",
                        return_url = _config["YOOKASSA_RETURN_URL"] ?? "https://zettechavio.ru/bookings"
                    },
                    capture = true,
                    description = description,
                    metadata = new
                    {
                        booking_id = bookingId,
                        booking_reference = booking.BookingReference
                    }
                };

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
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

        public async Task<Payment?> GetPaymentAsync(int paymentId)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Платеж с ID {PaymentId} не найден", paymentId);
                    return null;
                }
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при получении платежа: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> HandleWebhookAsync(string jsonData)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                {
                    _logger.LogWarning("Webhook получил пустые данные");
                    return false;
                }

                // Парсим JSON от YooKassa
                var notification = JsonConvert.DeserializeObject<dynamic>(jsonData);
                string yooKassaPaymentId = notification["object"]["id"];
                string status = notification["object"]["status"];

                // Находим платеж в БД
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.YooKassaPaymentId == yooKassaPaymentId);

                if (payment == null)
                {
                    _logger.LogWarning("Платеж {PaymentId} не найден в БД", yooKassaPaymentId);
                    return false;
                }

                // Если уже обработан - не обновляем повторно (идемпотентность)
                if (payment.Status != Payment.PaymentStatus.Pending)
                {
                    _logger.LogInformation("Платеж {PaymentId} уже обработан со статусом {Status}", 
                        yooKassaPaymentId, payment.Status);
                    return true;
                }

                // Обновляем статус платежа
                switch (status)
                {
                    case "succeeded":
                        payment.Status = Payment.PaymentStatus.Succeeded;
                        payment.UpdatedAt = DateTime.UtcNow;

                        // Обновляем бронирование
                        var booking = await _context.Bookings.FindAsync(payment.BookingId);
                        if (booking != null)
                        {
                            booking.Status = BookingStatus.Confirmed;
                            booking.UpdatedAt = DateTime.UtcNow;
                        }

                        _logger.LogInformation("Платеж {PaymentId} успешно обработан", yooKassaPaymentId);
                        break;

                    case "failed":
                    case "canceled":
                        payment.Status = Payment.PaymentStatus.Failed;
                        payment.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Платеж {PaymentId} отклонён со статусом {Status}", 
                            yooKassaPaymentId, status);
                        break;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обработке webhook: {ex.Message}");
                return false;
            }
        }
    }
}

