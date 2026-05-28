using Microsoft.EntityFrameworkCore;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;
using System.Net;
using System.Net.Mail;

namespace ZetTechAvio1._0.Services
{
    public interface IBookingsService
    {
        Task<BookingResponse?> CreateBookingAsync(int userId, CreateBookingRequest request);
        Task<List<BookingResponse>> GetUserBookingsAsync(int userId);
        Task<BookingResponse?> GetBookingAsync(int bookingId);

    }
    public interface IConfirmationService
    {
        Task<bool> GenerateCodeAsync(string email, HttpResponse response);
        Task<bool> VerifyCodeAsync(string email, string code, HttpRequest request, HttpResponse response);
    }

    public class ConfirmationService : IConfirmationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ConfirmationService> _logger;
        private readonly IEmailService _emailService;
        public ConfirmationService(ApplicationDbContext dbContext, IConfiguration config, IWebHostEnvironment env, ILogger<ConfirmationService> logger, IEmailService emailService)
        {
            _dbContext = dbContext;
            _config = config;
            _env = env;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<bool> GenerateCodeAsync(string email, HttpResponse response)
        {
            // Генерация 6-значного кода
            string code = new Random().Next(100000, 999999).ToString();

            // Заменяем мешающие символы @ и . на подчёркивание
            var safeCookieName = $"ConfirmationCode_{email.Replace("@", "_").Replace(".", "_")}";

            response.Cookies.Append(safeCookieName, code,
            new CookieOptions
            {
                Secure = !_env.IsDevelopment(),
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(10)
            });

            try
            {
                var emailBody = $"Ваш код подтверждения: {code}\nКод действителен 10 минут.";
                var emailSent = await _emailService.SendEmailAsync(email,
                    "Код подтверждения ZetTechAvio",
                    emailBody,
                    isHtml: false);

                if (!emailSent)
                {
                    _logger.LogWarning("Не удалось отправить код подтверждения на {Email}", email);
                }
                else
                {
                    _logger.LogInformation("Код подтверждения отправлен на {Email}", email);
                }
                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка отправки письма подтверждения на {Email}", email);
                return false;
            }
        }

        public Task<bool> VerifyCodeAsync(string email, string code, HttpRequest request, HttpResponse response)
        {
            // Пытаемся получить куки
            var safeCookieName = $"ConfirmationCode_{email.Replace("@", "_").Replace(".", "_")}";

            if (!request.Cookies.TryGetValue(safeCookieName, out var storedCode))
                return Task.FromResult(false);  // куки не найдена или истекла

            if (storedCode != code)
                return Task.FromResult(false);  // коды не совпадают

            // Удаляем куки после успешной проверки
            response.Cookies.Delete(safeCookieName);

            return Task.FromResult(true);
        }
    }

    public class BookingsService : IBookingsService
    {
        private readonly ApplicationDbContext _dbContext;

        public BookingsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<BookingResponse?> CreateBookingAsync(int userId, CreateBookingRequest request)
        {
            try
            {
                // Получаем тариф и рейс
                var fare = await _dbContext.Fares.FirstOrDefaultAsync(f => f.Id == request.FareId && f.FlightId == request.FlightId);
                if (fare == null)
                    return null;

                if (fare.SeatsAvailable < request.Quantity)
                    throw new InvalidOperationException("Недостаточно мест");

                if (request.Passengers == null || request.Passengers.Count < request.Quantity)
                    throw new InvalidOperationException("Недостаточно информации о пассажирах");

                var adultCount = request.Passengers.Count(p => string.Equals(p.PassengerType, nameof(PassengerType.Adult), StringComparison.OrdinalIgnoreCase));
                var childCount = request.Passengers.Count(p => string.Equals(p.PassengerType, nameof(PassengerType.Child), StringComparison.OrdinalIgnoreCase)
                                                            || string.Equals(p.PassengerType, nameof(PassengerType.Infant), StringComparison.OrdinalIgnoreCase));

                if (childCount > 0 && adultCount == 0)
                    throw new InvalidOperationException("Ребёнок не может лететь без взрослого");

                // Создаём бронирование
                var booking = new Booking
                {
                    UserId = userId,
                    BookingReference = GenerateBookingReference(),
                    Status = BookingStatus.Created,
                    Tickets = new List<Ticket>()
                };

                // Создаём билеты для каждого пассажира
                decimal totalPrice = 0;
                for (int i = 0; i < request.Quantity; i++)
                {
                    if (i >= request.Passengers.Count)
                        throw new InvalidOperationException("Недостаточно информации о пассажирах");

                    var passenger = request.Passengers[i];
                    if (passenger == null || string.IsNullOrWhiteSpace(passenger.FullName))
                        throw new InvalidOperationException($"Укажите полное имя для пассажира {i + 1}");

                    if (!Enum.TryParse<PassengerType>(passenger.PassengerType, true, out var passengerType))
                        throw new InvalidOperationException($"Неверный тип пассажира для пассажира {i + 1}");

                    if (passengerType == PassengerType.Adult)
                    {
                        if (string.IsNullOrWhiteSpace(passenger.PassportSeries) || passenger.PassportSeries.Length != 4
                            || string.IsNullOrWhiteSpace(passenger.PassportNumber) || passenger.PassportNumber.Length != 6)
                        {
                            throw new InvalidOperationException($"Для взрослого пассажира {i + 1} требуется серия и номер паспорта");
                        }
                    }
                    else
                    {
                        if ((!string.IsNullOrWhiteSpace(passenger.PassportSeries) && passenger.PassportSeries.Length != 4)
                            || (!string.IsNullOrWhiteSpace(passenger.PassportNumber) && passenger.PassportNumber.Length != 6))
                        {
                            throw new InvalidOperationException($"Серия и номер паспорта ребёнка должны быть заполнены корректно или оставлены пустыми для пассажира {i + 1}");
                        }
                    }

                    var passengerPrice = passengerType == PassengerType.Child || passengerType == PassengerType.Infant
                        ? Math.Round(fare.Price * 0.7M, 2)
                        : fare.Price;

                    var ticket = new Ticket
                    {
                        FlightId = request.FlightId,
                        FareId = request.FareId,
                        TicketNumber = GenerateTicketNumber(booking.BookingReference, i + 1),
                        PassengerName = passenger.FullName,
                        PassengerType = passengerType,
                        PassportSeries = passenger.PassportSeries,
                        PassportNumber = passenger.PassportNumber,
                        Price = passengerPrice,
                        Status = TicketStatus.Active
                    };

                    booking.Tickets.Add(ticket);
                    totalPrice += passengerPrice;
                }

                booking.TotalAmount = totalPrice;

                // Сохраняем в БД
                _dbContext.Bookings.Add(booking);
                await _dbContext.SaveChangesAsync();

                // Обновляем количество доступных мест
                fare.SeatsAvailable -= request.Quantity;
                await _dbContext.SaveChangesAsync();

                return MapToResponse(booking);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при создании бронирования: {ex.Message}");
            }
        }

        public async Task<List<BookingResponse>> GetUserBookingsAsync(int userId)
        {
            var bookings = await _dbContext.Bookings
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Flight)
                        .ThenInclude(f => f.OriginAirport)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Flight)
                        .ThenInclude(f => f.DestAirport)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bookings.Select(MapToResponse).ToList();
        }

        public async Task<BookingResponse?> GetBookingAsync(int bookingId)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            return booking == null ? null : MapToResponse(booking);
        }



        private BookingResponse MapToResponse(Booking booking)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                BookingReference = booking.BookingReference,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt,
                Tickets = booking.Tickets.Select(t => new TicketResponse
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    PassengerName = t.PassengerName,
                    Price = t.Price,
                    Status = t.Status.ToString(),
                    FlightId = t.FlightId,
                    Flight = t.Flight != null ? new FlightResponse
                    {
                        Id = t.Flight.Id,
                        FlightNumber = t.Flight.FlightNumber,
                        DepartureTime = t.Flight.DepartureDt,
                        ArrivalTime = t.Flight.ArrivalDt,
                        Duration = TimeSpan.FromMinutes(t.Flight.DurationMinutes),
                        DepartureAirport = new AirportResponse
                        {
                            Id = t.Flight.OriginAirport?.Id ?? 0,
                            Code = t.Flight.OriginAirport?.Iata ?? "N/A",
                            City = t.Flight.OriginAirport?.City ?? "Unknown"
                        },
                        ArrivalAirport = new AirportResponse
                        {
                            Id = t.Flight.DestAirport?.Id ?? 0,
                            Code = t.Flight.DestAirport?.Iata ?? "N/A",
                            City = t.Flight.DestAirport?.City ?? "Unknown"
                        }
                    } : null
                }).ToList()
            };
        }

        private string GenerateBookingReference()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var result = new char[6];
            for (int i = 0; i < 6; i++)
                result[i] = chars[random.Next(chars.Length)];
            return new string(result);
        }

        private string GenerateTicketNumber(string bookingRef, int sequence)
        {
            return $"{bookingRef}-{sequence:D3}";
        }


    }

}
