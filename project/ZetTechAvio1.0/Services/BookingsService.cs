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

        public ConfirmationService(ApplicationDbContext dbContext, IConfiguration config, IWebHostEnvironment env, ILogger<ConfirmationService> logger)
        {
            _dbContext = dbContext;
            _config = config;
            _env = env;
            _logger = logger;
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


            // Отправка письма
            try
            {
                var smtpHost = _config["SmtpSettings:Host"] ?? _config["SMTP_HOST"];
                var smtpPortStr = _config["SmtpSettings:Port"] ?? _config["SMTP_PORT"];
                var senderEmail = _config["SmtpSettings:Email"] ?? _config["SMTP_USER"];
                var senderPassword = _config["SmtpSettings:Password"] ?? _config["SMTP_PASSWORD"];

                // Debug логирование
                _logger.LogInformation($"SMTP Debug: Host={smtpHost}, Port={smtpPortStr}, Email='{senderEmail}', HasPassword={!string.IsNullOrWhiteSpace(senderPassword)}");

                // Валидация SMTP параметров
                if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
                {
                    _logger.LogWarning($"SMTP параметры не установлены. Host={smtpHost}, Email={senderEmail}. Email не отправлен.");
                    return false;
                }

                // Безопасный парсинг порта
                if (!int.TryParse(smtpPortStr, out int smtpPort))
                {
                    smtpPort = 25; // Дефолтный порт SMTP
                }

                using (var smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    
                    // Настройка SSL/TLS в зависимости от порта
                    if (smtpPort == 25)
                    {
                        smtp.EnableSsl = false; // Порт 25 - обычный SMTP без SSL
                    }
                    else if (smtpPort == 587)
                    {
                        smtp.EnableSsl = true; // Порт 587 - STARTTLS
                    }
                    else if (smtpPort == 465)
                    {
                        smtp.EnableSsl = true; // Порт 465 - SMTPS (SSL)
                    }
                    else
                    {
                        smtp.EnableSsl = true; // Для других портов включаем SSL
                    }

                    _logger.LogInformation($"SMTP подключение: Host={smtpHost}, Port={smtpPort}, SSL={smtp.EnableSsl}, User={senderEmail}");
                    
                    _logger.LogInformation($"Создание MailMessage с From={senderEmail}");
                    var mail = new MailMessage
                    {
                        From = new MailAddress(senderEmail),
                        Subject = "Код подтверждения ZetTechAvio",
                        Body = $"Ваш код подтверждения: {code}\nКод действителен 10 минут.",
                        IsBodyHtml = false
                    };
                    mail.To.Add(email);

                    await smtp.SendMailAsync(mail);
                    _logger.LogInformation($"Email отправлен на {email}");
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                _logger.LogWarning($"Ошибка отправки письма: {ex.Message}");
                // Не выбрасываем исключение - бронирование уже создано
                Console.WriteLine($"Ошибка отправки письма: {ex.Message}");
            }

            return true;
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
                    var ticket = new Ticket
                    {
                        FlightId = request.FlightId,
                        FareId = request.FareId,
                        TicketNumber = GenerateTicketNumber(booking.BookingReference, i + 1),
                        PassengerName = passenger.FullName,
                        PassengerType = (PassengerType)Enum.Parse(typeof(PassengerType), passenger.PassengerType),
                        PassportSeries = passenger.PassportSeries,
                        PassportNumber = passenger.PassportNumber,
                        Price = fare.Price,
                        Status = TicketStatus.Active
                    };

                    booking.Tickets.Add(ticket);
                    totalPrice += fare.Price;
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
