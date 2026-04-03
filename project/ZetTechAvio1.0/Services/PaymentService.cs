// using YooKassa;
// using YooKassa.Model;
// using ZetTechAvio1._0.Data;
// using ZetTechAvio1._0.Models;

// namespace ZetTechAvio1._0.Services
// {
//     public interface IPaymentService
//     {
//         Task<Payment?> CreatePaymentAsync(int bookingId, string description);
//         Task<bool> HandleWebhookAsync(string jsonData);
//         Task<Payment?> GetPaymentAsync(int paymentId);
//     }
//     // Реализуйте методы для создания платежа, получения статуса и обработки уведомлений от Яндекс.Кассы

//     public class PaymentService : IPaymentService
//     {
//         private readonly ApplicationDbContext _context;
//         private readonly ILogger<PaymentService> _logger;
//         private readonly IConfiguration _config;
//         private readonly Client _yooKassaClient;

//         public PaymentService(ApplicationDbContext context, IConfiguration config, ILogger<PaymentService> logger)
//         {
//             _context = context;
//             _logger = logger;
//             _config = config;

//             string shopId = _config["YOOKASSA_SHOP_ID"] ?? throw new Exception("YOOKASSA_SHOP_ID не установлен");
//             string apiKey = _config["YOOKASSA_SECRET_KEY"] ?? throw new Exception("YOOKASSA_SECRET_KEY не установлен");

//             _yooKassaClient = new Client();
//             _yooKassaClient.SetAuth(shopId, apiKey);
//         }

//         /// <summary>
//         /// Создает платеж в YooKassa и сохраняет в БД
//         /// </summary>

//         public async Task<Payment?> CreatePaymentAsync(int bookingId, string description)
//         {
//             try
//             {
                
            
//             // 1. Получить бронирование

//             var booking = await _context.Bookings.FindAsync(bookingId);
//             if (booking == null)
//             {
//                 _logger.LogWarning("Бронирование с ID {BookingId} не найдено", bookingId);
//                 return null;
//             }

//             // 2. Создать платеж в YooKassa
//                 var createPaymentRequest = new CreatePaymentRequest
//                 {
//                     Amount = new MoneyValue
//                     {
//                         Value = booking.TotalAmount,
//                         Currency = "RUB"
//                     },
//                     Confirmation = new Confirmation
//                     {
//                         Type = ConfirmationType.Redirect,
//                         ReturnUrl = _config["YOOKASSA_RETURN_URL"] ?? "https://zettechavio.ru/bookings"
//                     },
//                     Capture = true,  // автоматически захватить платеж
//                     Description = description,
//                     Metadata = new Dictionary<string, object>
//                     {
//                         { "booking_id", bookingId },
//                         { "booking_reference", booking.BookingReference }
//                     }
//                 };

//                 var response = await _yooKassaClient.CreatePaymentAsync(createPaymentRequest);

//                 // 3. Сохранить платеж в БД
//                 var payment = new Payment
//                 {
//                     BookingId = bookingId,
//                     YooKassaPaymentId = response.Id,
//                     Amount = booking.TotalAmount,
//                     Status = "pending",
//                     ConfirmationUrl = response.Confirmation?.ConfirmationUrl,
//                     Description = description,
//                     CreatedAt = DateTime.UtcNow,
//                     UpdatedAt = DateTime.UtcNow
//                 };

//                 _context.Payments.Add(payment);
//                 await _context.SaveChangesAsync();

//                 _logger.LogInformation($"Платеж {response.Id} создан для бронирования {bookingId}");

//                 return payment;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError($"Ошибка при создании платежа: {ex.Message}");
//                 return null;
//             }
//         }
//         }
//     }

