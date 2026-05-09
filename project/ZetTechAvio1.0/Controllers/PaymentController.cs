using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Security.Claims;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IBookingsService _bookingsService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, IBookingsService bookingsService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _bookingsService = bookingsService;
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
                if (!await _bookingsService.IsBookingOwnedByUserAsync(request.BookingId, userId))
                    return Forbid();

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

        [HttpPost("webhook")]
        [AllowAnonymous]
        [EnableCors("AllowWebhook")]
        public async Task<IActionResult> Webhook()
        {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
                return BadRequest(new { message = "Пустое тело запроса" });

            JObject payload;
            try
            {
                payload = JsonConvert.DeserializeObject<JObject>(body) ?? new JObject();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Webhook: неверный JSON: {Message}", ex.Message);
                return BadRequest(new { message = "Неверный JSON" });
            }

            var paymentObject = payload.SelectToken("object.payment") as JObject ?? payload.SelectToken("object") as JObject;
            if (paymentObject == null)
                return BadRequest(new { message = "Не найден объект payment" });

            var yooKassaPaymentId = paymentObject.Value<string>("id");
            var bookingId = paymentObject.SelectToken("metadata.booking_id")?.Value<int?>();
            var eventType = payload.Value<string>("event") ?? payload.Value<string>("type");

            if (string.IsNullOrWhiteSpace(yooKassaPaymentId))
                return BadRequest(new { message = "Не найден payment id" });

            _logger.LogInformation("Webhook received: event={Event}, paymentId={PaymentId}, bookingId={BookingId}", eventType, yooKassaPaymentId, bookingId);

            if (!bookingId.HasValue)
            {
                _logger.LogWarning("Webhook payload не содержит metadata.booking_id");
                return BadRequest(new { message = "metadata.booking_id не найден" });
            }

            var payment = await _paymentService.VerifyAndUpdatePaymentStatusAsync(bookingId.Value, yooKassaPaymentId);
            if (payment == null)
                return StatusCode(500, new { message = "Ошибка обработки webhook" });

            return Ok(new { message = "Webhook processed", status = payment.Status.ToString() });
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
