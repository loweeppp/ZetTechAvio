using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Создать платеж для бронирования
        /// </summary>
        [HttpPost("create-payment")]
        [Authorize]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            // Получить userId из токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Не авторизован" });
            }

            if (request?.BookingId <= 0)
            {
                return BadRequest(new { message = "BookingId некорректен" });
            }

            try
            {
                // Проверка: пользователь создал это бронирование?
                // (Раскомментировать после реализации проверки доступа к бронированию)
                // var booking = await _bookingsService.GetBookingAsync(request.BookingId);
                // if (booking?.UserId != userId)
                //     return Forbid(new { message = "Только свои бронирования!" });

                string description = $"Оплата бронирования {request.BookingId}";
                var payment = await _paymentService.CreatePaymentAsync(request.BookingId, description);

                if (payment == null)
                {
                    return StatusCode(500, new { message = "Ошибка при создании платежа в YooKassa" });
                }

                _logger.LogInformation($"Платеж создан для бронирования {request.BookingId}");

                return Ok(new
                {
                    message = "Платеж создан",
                    paymentId = payment.Id,
                    confirmationUrl = payment.ConfirmationUrl,
                    amount = payment.TotalAmount,
                    status = payment.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при создании платежа: {ex.Message}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить данные платежа
        /// </summary>
        [HttpGet("{paymentId}")]
        [Authorize]
        public async Task<IActionResult> GetPayment(int paymentId)
        {
            if (paymentId <= 0)
            {
                return BadRequest(new { message = "PaymentId некорректен" });
            }

            try
            {
                var payment = await _paymentService.GetPaymentAsync(paymentId);

                if (payment == null)
                {
                    return NotFound(new { message = "Платеж не найден" });
                }

                return Ok(new
                {
                    id = payment.Id,
                    bookingId = payment.BookingId,
                    yooKassaPaymentId = payment.YooKassaPaymentId,
                    amount = payment.TotalAmount,
                    status = payment.Status.ToString(),
                    confirmationUrl = payment.ConfirmationUrl,
                    createdAt = payment.CreatedAt,
                    updatedAt = payment.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при получении платежа: {ex.Message}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Webhook от YooKassa с уведомлением о платеже
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                // Прочитать body
                using (StreamReader reader = new StreamReader(Request.Body))
                {
                    string jsonData = await reader.ReadToEndAsync();
                    _logger.LogInformation($"Webhook получен: {jsonData}");

                    var result = await _paymentService.HandleWebhookAsync(jsonData);

                    if (!result)
                    {
                        return StatusCode(400, new { status = "error" });
                    }

                    return Ok(new { status = "ok" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обработке webhook: {ex.Message}");
                return StatusCode(500, new { status = "error" });
            }
        }
    }

    public class CreatePaymentRequest
    {
        public int BookingId { get; set; }
    }
}
