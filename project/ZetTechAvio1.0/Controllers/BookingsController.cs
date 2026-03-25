using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZetTechAvio1._0.Models;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingsService _bookingsService;
        private readonly ILogger<BookingsController> _logger;
        private readonly IConfirmationService _confirmationService;

        public BookingsController(IBookingsService bookingsService, ILogger<BookingsController> logger, IConfirmationService confirmationService)
        {
            _bookingsService = bookingsService;
            _logger = logger;
            _confirmationService = confirmationService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                // Получаем userId из токена
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized("Пользователь не идентифицирован");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var booking = await _bookingsService.CreateBookingAsync(userId, request);
                if (booking == null)
                    return NotFound("Рейс или тариф не найден");

                return Ok(booking);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Ошибка при создании бронирования: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Неожиданная ошибка: {ex.Message}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserBookings(int userId)
        {
            try
            {
                // Проверяем, что пользователь запрашивает только свои бронирования
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                    return Unauthorized();

                if (currentUserId != userId)
                    return Forbid("Вы можете просматривать только свои бронирования");

                var bookings = await _bookingsService.GetUserBookingsAsync(userId);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при получении бронирований: {ex.Message}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("request-confirmation")]
        public async Task<IActionResult> RequestConfirmation([FromBody] RequestConfirmationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(new { message = "Email обязателен" });

            await _confirmationService.GenerateCodeAsync(request.Email, Response);
            return Ok(new { success = true, message = "Код подтверждения отправлен на почту" });
        }

        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyConfirmationCode([FromBody] VerifyCodeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrEmpty(request.Code))
                return BadRequest(new { message = "Код подтверждения обязателен" });

            var isValid = await _confirmationService.VerifyCodeAsync(request.Email, request.Code, Request, Response);

            if (!isValid)
                return BadRequest(new { message = "Неверный или истекший код" });

            return Ok(new { success = true, message = "Код подтверждения верный" });
        }

        [HttpGet("booking/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetBooking(int bookingId)
        {
            try
            {
                var booking = await _bookingsService.GetBookingAsync(bookingId);
                if (booking == null)
                    return NotFound("Бронирование не найдено");

                // Проверяем доступ
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Здесь нужно добавить проверку, что бронирование принадлежит текущему пользователю
                    // TODO: Вернуть информацию о пользователе вместе с бронированием
                }

                return Ok(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при получении бронирования: {ex.Message}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        public class RequestConfirmationRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public class VerifyCodeRequest
        {
            public string Code { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}
