using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

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
        Task<bool> VerifyCodeAsync(string email, string code, HttpRequest request, HttpResponse response, bool deleteOnSuccess = true);
    }

    public class ConfirmationService : IConfirmationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ConfirmationService> _logger;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public ConfirmationService(
            ApplicationDbContext dbContext,
            IConfiguration config,
            IWebHostEnvironment env,
            ILogger<ConfirmationService> logger,
            IEmailService emailService,
            IMemoryCache cache)
        {
            _dbContext = dbContext;
            _config = config;
            _env = env;
            _logger = logger;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<bool> GenerateCodeAsync(string email, HttpResponse response)
        {
            string code = new Random().Next(100000, 999999).ToString();
            var cacheKey = GetSafeCacheKey(email);
            _cache.Set(cacheKey, code, TimeSpan.FromMinutes(10));

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

        public Task<bool> VerifyCodeAsync(string email, string code, HttpRequest request, HttpResponse response, bool deleteOnSuccess = true)
        {
            var cacheKey = GetSafeCacheKey(email);

            if (!_cache.TryGetValue(cacheKey, out string? storedCode))
                return Task.FromResult(false);

            if (storedCode != code)
                return Task.FromResult(false);

            if (deleteOnSuccess)
            {
                _cache.Remove(cacheKey);
            }

            return Task.FromResult(true);
        }

        private static string GetSafeCacheKey(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail));
            return $"ConfirmationCode_{Convert.ToHexString(hash)}";
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

                var existingTicketsOnFlight = await _dbContext.Tickets
                    .Where(t => t.FlightId == request.FlightId
                             && t.Booking != null && t.Booking.UserId == userId
                             && t.Status != TicketStatus.Cancelled)
                    .CountAsync();

                if (existingTicketsOnFlight + request.Quantity > 5)
                    throw new InvalidOperationException("Нельзя купить более 5 билетов на один рейс.");

                var selectedSeats = await AllocateSeatsAsync(request.FlightId, request.Quantity, fare.Class);

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

                    var selectedSeat = selectedSeats[i];

                    var ticket = new Ticket
                    {
                        FlightId = request.FlightId,
                        FareId = request.FareId,
                        SeatId = selectedSeat.Id,
                        Seat = selectedSeat,
                        TicketNumber = GenerateTicketNumber(booking.BookingReference, i + 1),
                        PassengerName = passenger.FullName,
                        PassengerType = passengerType,
                        PassportSeries = passenger.PassportSeries,
                        PassportNumber = passenger.PassportNumber,
                        Price = passengerPrice,
                        Status = TicketStatus.Active
                    };

                    selectedSeat.Status = SeatStatus.Booked;
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
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Seat)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bookings.Select(MapToResponse).ToList();
        }

        public async Task<BookingResponse?> GetBookingAsync(int bookingId)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Seat)
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
                    SeatNumber = t.Seat?.SeatNumber ?? string.Empty,
                    FlightId = t.FlightId,
                    Flight = t.Flight != null ? new FlightResponse
                    {
                        Id = t.Flight.Id,
                        FlightNumber = t.Flight.FlightNumber,
                        DepartureTime = t.Flight.DepartureDt,
                        ArrivalTime = t.Flight.ArrivalDt,
                        Duration = TimeSpan.FromMinutes(t.Flight.DurationMinutes),
                        Status = t.Flight.Status.ToString(),
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

        private async Task<List<Seat>> AllocateSeatsAsync(int flightId, int quantity, Fare.Fare_class fareClass)
        {
            var preferredSeatClass = Enum.TryParse<SeatClass>(fareClass.ToString(), true, out var seatClass)
                ? seatClass
                : SeatClass.Economy;

            await EnsureSeatsExistAsync(flightId);

            var availableSeats = await _dbContext.Seats
                .Where(s => s.FlightId == flightId && s.Status == SeatStatus.Available && s.SeatClass == preferredSeatClass)
                .OrderBy(s => s.SeatNumber)
                .ToListAsync();

            if (availableSeats.Count < quantity)
            {
                availableSeats = await _dbContext.Seats
                    .Where(s => s.FlightId == flightId && s.Status == SeatStatus.Available)
                    .OrderBy(s => s.SeatNumber)
                    .ToListAsync();
            }

            if (availableSeats.Count < quantity)
                throw new InvalidOperationException("Недостаточно доступных мест на этом рейсе.");

            var groupedByRow = availableSeats
                .Select(s => new
                {
                    Seat = s,
                    Row = ParseSeatRow(s.SeatNumber),
                    Column = ParseSeatColumn(s.SeatNumber)
                })
                .Where(x => x.Row.HasValue)
                .GroupBy(x => x.Row.Value)
                .Where(g => g.Count() >= quantity)
                .ToList();

            var random = new Random();
            if (groupedByRow.Any())
            {
                var chosenGroup = groupedByRow[random.Next(groupedByRow.Count)];
                return chosenGroup
                    .OrderBy(x => x.Column)
                    .Take(quantity)
                    .Select(x => x.Seat)
                    .ToList();
            }

            var shuffled = availableSeats.OrderBy(_ => random.Next()).ToList();
            return shuffled.Take(quantity).ToList();
        }

        private async Task EnsureSeatsExistAsync(int flightId)
        {
            if (await _dbContext.Seats.AnyAsync(s => s.FlightId == flightId))
                return;

            var flight = await _dbContext.Flights
                .Include(f => f.Aircraft)
                .Include(f => f.Fares)
                .FirstOrDefaultAsync(f => f.Id == flightId);

            if (flight == null)
                throw new InvalidOperationException("Рейс не найден при распределении мест.");

            if (flight.Fares == null || !flight.Fares.Any())
                return;

            var seatDefinitions = flight.Fares
                .OrderBy(f => f.Class == Fare.Fare_class.First ? 0 : f.Class == Fare.Fare_class.Business ? 1 : 2)
                .Select(f => new { SeatClass = GetSeatClassFromFare(f.Class), SeatsCount = Math.Max(f.SeatsAvailable, 0) })
                .Where(x => x.SeatsCount > 0)
                .ToList();

            if (!seatDefinitions.Any())
                return;

            var seats = new List<Seat>();
            var nextRow = 1;
            foreach (var definition in seatDefinitions)
            {
                seats.AddRange(GenerateSeatLayout(flight.Id, definition.SeatClass, definition.SeatsCount, ref nextRow));
            }

            if (seats.Any())
            {
                await _dbContext.Seats.AddRangeAsync(seats);
                await _dbContext.SaveChangesAsync();
            }
        }

        private static SeatClass GetSeatClassFromFare(Fare.Fare_class fareClass)
        {
            return Enum.TryParse<SeatClass>(fareClass.ToString(), true, out var seatClass)
                ? seatClass
                : SeatClass.Economy;
        }

        private static IEnumerable<Seat> GenerateSeatLayout(int flightId, SeatClass seatClass, int count, ref int rowCounter)
        {
            const string seatColumns = "ABCDEF";
            var seats = new List<Seat>(count);

            for (int i = 0; i < count; i++)
            {
                var row = rowCounter + (i / seatColumns.Length);
                var column = seatColumns[i % seatColumns.Length].ToString();
                seats.Add(new Seat
                {
                    FlightId = flightId,
                    SeatNumber = $"{row}{column}",
                    SeatClass = seatClass,
                    Status = SeatStatus.Available,
                });
            }

            rowCounter += (int)Math.Ceiling(count / (double)seatColumns.Length);
            return seats;
        }

        private static int? ParseSeatRow(string seatNumber)
        {
            if (string.IsNullOrWhiteSpace(seatNumber))
                return null;

            var digits = new string(seatNumber.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var row) ? row : null;
        }

        private static string ParseSeatColumn(string seatNumber)
        {
            if (string.IsNullOrWhiteSpace(seatNumber))
                return string.Empty;

            var column = new string(seatNumber.SkipWhile(char.IsDigit).ToArray());
            return column.ToUpperInvariant();
        }
    }

}
