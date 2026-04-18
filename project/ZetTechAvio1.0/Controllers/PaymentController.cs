using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
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
            if (!this.TryGetUserId(out int userId))
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
                    yooKassaPaymentId = payment.YooKassaPaymentId,
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
        /// Проверить статус платежа в YooKassa и обновить его локально
        /// Используется для тестирования на localhost (веб-хуки не могут достичь localhost)
        /// </summary>
        [HttpPost("verify-status")]
        [Authorize]
        public async Task<IActionResult> VerifyPaymentStatus([FromBody] VerifyPaymentRequest request)
        {
            if (request?.BookingId <= 0 || string.IsNullOrWhiteSpace(request?.YooKassaPaymentId))
            {
                return BadRequest(new { message = "BookingId и YooKassaPaymentId обязательны" });
            }

            try
            {
                _logger.LogInformation($"[VERIFY] Проверка платежа {request.YooKassaPaymentId} для бронирования {request.BookingId}");

                var payment = await _paymentService.VerifyAndUpdatePaymentStatusAsync(request.BookingId, request.YooKassaPaymentId);

                if (payment == null)
                {
                    return StatusCode(500, new { message = "Ошибка при проверке статуса платежа" });
                }

                _logger.LogInformation($"[VERIFY] Платеж обновлён: {payment.YooKassaPaymentId}, Status={payment.Status}");

                return Ok(new
                {
                    message = "Статус платежа обновлён",
                    paymentId = payment.Id,
                    status = payment.Status.ToString(),
                    yooKassaPaymentId = payment.YooKassaPaymentId,
                    bookingId = payment.BookingId,
                    amount = payment.TotalAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[VERIFY] Ошибка: {ex.Message}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }

    public class CreatePaymentRequest
    {
        public int BookingId { get; set; }
    }

    public class VerifyPaymentRequest
    {
        public int BookingId { get; set; }
        public string YooKassaPaymentId { get; set; }
    }
}
