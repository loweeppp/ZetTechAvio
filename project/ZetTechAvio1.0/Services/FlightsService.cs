using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public class DeleteFlightResult
    {
        public bool WasDeleted { get; set; }
        public int TicketCount { get; set; }
    }

    public interface IFlightsService
    {
        Task<List<FlightDto>> GetAllFlightsAsync();
        Task<List<FlightDto>> SearchFlightsAsync(string from, string to, string date);
        Task<Flight?> GetFlightByIdAsync(int id);
        Task<Flight> CreateFlightAsync(Flight flight);
        Task<Flight?> UpdateFlightAsync(int id, Flight updatedFlight, List<FareClassRequest>? fareClasses = null);
        Task<DeleteFlightResult> DeleteFlightAsync(int id);
        Task<Flight?> CancelFlightAsync(int id);
        Task<List<Fare>> GetFlightFaresAsync(int flightId);
        Task<List<Airport>> GetAirportsAsync();
        Task<Airport?> GetAirportByIdAsync(int airportId);
        Task<List<Airline>> GetAirlinesAsync();
        Task<Airline?> GetAirlineByIdAsync(int airlineId);
        Task<string?> ValidateAirportRouteAsync(int originAirportId, int destAirportId);
        Task<string> GenerateFlightNumberAsync(string airlinePrefix);
        Task<List<Aircraft>> GetAircraftsAsync();
        Task<int> GetFlightTicketCountAsync(int flightId);
        Task<List<FlightTicketResponse>> GetFlightTicketsAsync(int flightId);
        Task<int> MarkPastFlightsCompletedAsync();
        Task<List<Flight>> CreateScheduledFlightsAsync(FlightScheduleRequest request);
    }

    public class FlightsService : IFlightsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService? _emailService;

        public FlightsService(ApplicationDbContext dbContext, IEmailService? emailService = null)
        {
            _dbContext = dbContext;
            _emailService = emailService;
        }

        public async Task<List<FlightDto>> GetAllFlightsAsync()
        {
            var flights = await _dbContext.Flights
                .Include(f => f.OriginAirport)
                .Include(f => f.DestAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.Fares)
                .Include(f => f.Tickets)
                .ToListAsync();

            return flights.Select(MapToDto).ToList();
        }
        private FlightDto MapToDto(Flight flight)
        {
            var remainingSeats = flight.Fares?.Sum(f => f.SeatsAvailable) ?? 0;
            var ticketCount = flight.Tickets?.Count ?? 0;
            var maxSeats = ticketCount + remainingSeats;

            return new FlightDto
            {
                Id = flight.Id,
                FlightNumber = flight.FlightNumber,
                DurationMinutes = flight.DurationMinutes,
                DepartureDt = flight.DepartureDt,
                ArrivalDt = flight.ArrivalDt,
                MinPrice = flight.Fares != null && flight.Fares.Any() ? flight.Fares.Min(f => f.Price) : 0,
                // если есть тариф с багажом то выводить "Багаж включен", если нет то "Багаж не включен"
                BaggageInfo = flight.Fares != null && flight.Fares.Any(f => f.BaggageIncluded) ? "Багаж включен" : "Багаж не включен",
                OriginAirport = flight.OriginAirport,
                DestAirport = flight.DestAirport,
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                TicketCount = ticketCount,
                RemainingSeats = remainingSeats,
                MaxSeats = maxSeats,
                Status = flight.Status.ToString()
            };
        }
        public async Task<List<FlightDto>> SearchFlightsAsync(string from, string to, string date)
        {
            try
            {
                var airports = await _dbContext.Airports.ToListAsync();
                var fromAirportCodes = GetAirportCodesForCityGroup(from, airports);
                var toAirportCodes = GetAirportCodesForCityGroup(to, airports);

                IQueryable<Flight> query = _dbContext.Flights
                    .Include(f => f.OriginAirport)
                    .Include(f => f.DestAirport)
                    .Include(f => f.Aircraft)
                    .Include(f => f.Fares)
                    .Include(f => f.Tickets);

                // Filter BEFORE ToListAsync (on database, not in memory!)
                if (!string.IsNullOrEmpty(from))
                {
                    if (fromAirportCodes.Count > 1)
                    {
                        query = query.Where(f => fromAirportCodes.Contains(f.OriginAirport.Iata));
                    }
                    else
                    {
                        var fromLower = from.ToLower();
                        query = query.Where(f =>
                            f.OriginAirport.Iata.ToLower().Contains(fromLower) ||
                            f.OriginAirport.City.ToLower().Contains(fromLower));
                    }
                }

                if (!string.IsNullOrEmpty(to))
                {
                    if (toAirportCodes.Count > 1)
                    {
                        query = query.Where(f => toAirportCodes.Contains(f.DestAirport.Iata));
                    }
                    else
                    {
                        var toLower = to.ToLower();
                        query = query.Where(f =>
                            f.DestAirport.Iata.ToLower().Contains(toLower) ||
                            f.DestAirport.City.ToLower().Contains(toLower));
                    }
                }

                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
                {
                    query = query.Where(f => f.DepartureDt.Date == parsedDate.Date);
                }

                // Only return flights that are still active/current in search results.
                query = query.Where(f => f.Status != FlightStatus.Cancelled && f.Status != FlightStatus.Completed);

                var flights = await query.ToListAsync();
                return flights.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SearchFlightsAsync error: {ex.Message}");
                return new List<FlightDto>();
            }
        }

        private static IReadOnlyCollection<string> GetAirportCodesForCityGroup(string? input, IEnumerable<Airport> airports)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Array.Empty<string>();

            var normalizedInput = NormalizeAirportCode(input);
            if (normalizedInput == null)
                return Array.Empty<string>();

            var exactAirport = airports.FirstOrDefault(a => string.Equals(a.Iata, normalizedInput, StringComparison.OrdinalIgnoreCase));
            if (exactAirport == null)
                return new[] { normalizedInput };

            List<string> sameCityCodes = airports
                .Where(a => !string.IsNullOrWhiteSpace(a.City) && string.Equals(a.City, exactAirport.City, StringComparison.OrdinalIgnoreCase))
                .Select(a => NormalizeAirportCode(a.Iata))
                .Where(code => code != null)
                .Select(code => code!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return sameCityCodes.Count > 1 ? sameCityCodes : new List<string> { normalizedInput };
        }

        private static string? NormalizeAirportCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var trimmed = code.Trim();
            return trimmed.Length == 3 ? trimmed.ToUpperInvariant() : null;
        }

        public async Task<List<Airport>> GetAirportsAsync()
        {
            return await _dbContext.Airports
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<Airport?> GetAirportByIdAsync(int airportId)
        {
            return await _dbContext.Airports.FindAsync(airportId);
        }

        public async Task<Airline?> GetAirlineByIdAsync(int airlineId)
        {
            return await _dbContext.Airlines.FindAsync(airlineId);
        }

        public async Task<string?> ValidateAirportRouteAsync(int originAirportId, int destAirportId)
        {
            if (originAirportId <= 0 || destAirportId <= 0)
                return "Выберите оба аэропорта отправления и прибытия.";

            if (originAirportId == destAirportId)
                return "Аэропорт отправления и прибытия не могут быть одинаковыми.";

            var origin = await _dbContext.Airports.FindAsync(originAirportId);
            var dest = await _dbContext.Airports.FindAsync(destAirportId);

            if (origin == null || dest == null)
                return "Один из выбранных аэропортов не найден.";

            if (string.Equals(origin.City?.Trim(), dest.City?.Trim(), StringComparison.OrdinalIgnoreCase))
                return "Аэропорт вылета и прибытия не могут находиться в одном городе.";

            return null;
        }

        public async Task<string> GenerateFlightNumberAsync(string airlinePrefix)
        {
            var prefix = airlinePrefix?.Trim().ToUpperInvariant() ?? throw new ArgumentException("Префикс авиакомпании не задан.");

            var existingSuffixes = await _dbContext.Flights
                .Where(f => !string.IsNullOrEmpty(f.FlightNumber) && f.FlightNumber.StartsWith(prefix))
                .Select(f => f.FlightNumber!.Substring(prefix.Length))
                .ToListAsync();

            var usedNumbers = new HashSet<int>();
            foreach (var suffix in existingSuffixes)
            {
                if (int.TryParse(suffix, out var number) && number >= 100)
                {
                    usedNumbers.Add(number);
                }
            }

            for (var candidate = 100; candidate <= 9999; candidate++)
            {
                if (!usedNumbers.Contains(candidate))
                    return prefix + candidate;
            }

            throw new InvalidOperationException($"Не удалось сгенерировать уникальный номер рейса для {prefix}.");
        }

        public async Task<List<Airline>> GetAirlinesAsync()
        {
            return await _dbContext.Airlines
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<List<Aircraft>> GetAircraftsAsync()
        {
            return await _dbContext.Aircrafts
                .OrderBy(a => a.Manufacturer)
                .ThenBy(a => a.Model)
                .ToListAsync();
        }

        public async Task<Flight?> GetFlightByIdAsync(int id)
        {
            return await _dbContext.Flights
                .Include(f => f.Airline)
                .Include(f => f.Aircraft)
                .Include(f => f.OriginAirport)
                .Include(f => f.DestAirport)
                .Include(f => f.Fares)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Flight> CreateFlightAsync(Flight flight)
        {
            var routeError = await ValidateAirportRouteAsync(flight.OriginAirportId, flight.DestAirportId);
            if (routeError != null)
            {
                throw new ArgumentException(routeError);
            }

            if (string.IsNullOrWhiteSpace(flight.FlightNumber))
            {
                var airline = await _dbContext.Airlines.FindAsync(flight.AirlineId)
                    ?? throw new InvalidOperationException("Авиакомпания не найдена для генерации номера рейса.");

                flight.FlightNumber = await GenerateFlightNumberAsync(airline.IataCode);
            }

            _dbContext.Flights.Add(flight);
            await _dbContext.SaveChangesAsync();
            return flight;
        }

        public async Task<List<Flight>> CreateScheduledFlightsAsync(FlightScheduleRequest request)
        {
            var routeError = await ValidateAirportRouteAsync(request.OriginAirportId, request.DestAirportId);
            if (routeError != null)
                throw new ArgumentException(routeError);

            if (request.StartDate.Date > request.EndDate.Date)
                throw new ArgumentException("Дата начала не может быть позже даты окончания.");

            var airline = await _dbContext.Airlines.FindAsync(request.AirlineId);
            if (airline == null)
            {
                throw new ArgumentException("Авиакомпания не найдена для генерации номера рейса.");
            }

            var prefix = airline.IataCode?.Trim().ToUpperInvariant() ?? throw new InvalidOperationException("Префикс авиакомпании отсутствует.");
            var existingSuffixes = await _dbContext.Flights
                .Where(f => !string.IsNullOrEmpty(f.FlightNumber) && f.FlightNumber.StartsWith(prefix))
                .Select(f => f.FlightNumber!.Substring(prefix.Length))
                .ToListAsync();

            var usedNumbers = new HashSet<int>();
            foreach (var suffix in existingSuffixes)
            {
                if (int.TryParse(suffix, out var number) && number >= 100)
                {
                    usedNumbers.Add(number);
                }
            }

            var nextCandidate = 100;
            if (!string.IsNullOrWhiteSpace(request.FlightNumber)
                && request.FlightNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(request.FlightNumber[prefix.Length..], out var manualNumber)
                && manualNumber >= 100)
            {
                nextCandidate = manualNumber;
            }

            string GetNextFlightNumber()
            {
                while (usedNumbers.Contains(nextCandidate))
                {
                    nextCandidate++;
                }

                usedNumbers.Add(nextCandidate);
                return prefix + nextCandidate++;
            }

            if (string.IsNullOrWhiteSpace(request.FlightNumber)
                || !request.FlightNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || !int.TryParse(request.FlightNumber[prefix.Length..], out var startNumber)
                || startNumber < 100)
            {
                request.FlightNumber = GetNextFlightNumber();
            }
            else if (request.FlightNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                     && int.TryParse(request.FlightNumber[prefix.Length..], out var parsedNumber)
                     && parsedNumber >= 100)
            {
                if (usedNumbers.Contains(parsedNumber))
                {
                    request.FlightNumber = GetNextFlightNumber();
                }
                else
                {
                    usedNumbers.Add(parsedNumber);
                    nextCandidate = parsedNumber + 1;
                }
            }

            var currentFlightNumber = request.FlightNumber;

            var targetDays = request.Weekdays
                .Select(day => day switch
                {
                    0 => DayOfWeek.Monday,
                    1 => DayOfWeek.Tuesday,
                    2 => DayOfWeek.Wednesday,
                    3 => DayOfWeek.Thursday,
                    4 => DayOfWeek.Friday,
                    5 => DayOfWeek.Saturday,
                    6 => DayOfWeek.Sunday,
                    _ => throw new ArgumentException($"Неверный день недели: {day}")
                })
                .Distinct()
                .ToList();

            var departureTime = TimeSpan.Parse(request.DepartureTime);
            var arrivalTime = TimeSpan.Parse(request.ArrivalTime);

            var flightsToCreate = new List<Flight>();
            var currentDate = request.StartDate.Date;

            while (currentDate <= request.EndDate.Date)
            {
                if (targetDays.Contains(currentDate.DayOfWeek))
                {
                    var departureDt = currentDate.Add(departureTime);
                    var arrivalDt = currentDate.Add(arrivalTime);
                    if (arrivalDt <= departureDt)
                    {
                        arrivalDt = arrivalDt.AddDays(1);
                    }

                    var duplicate = await _dbContext.Flights.AnyAsync(f =>
                        f.FlightNumber == currentFlightNumber && f.DepartureDt == departureDt);

                    if (!duplicate)
                    {
                        var flight = new Flight
                        {
                            FlightNumber = currentFlightNumber,
                            AirlineId = request.AirlineId,
                            AircraftId = request.AircraftId,
                            OriginAirportId = request.OriginAirportId,
                            DestAirportId = request.DestAirportId,
                            DepartureDt = departureDt,
                            ArrivalDt = arrivalDt,
                            DurationMinutes = (int)(arrivalDt - departureDt).TotalMinutes,
                            Status = Enum.Parse<FlightStatus>(request.Status ?? "Scheduled")
                        };

                        if (request.FareClasses != null && request.FareClasses.Any())
                        {
                            flight.Fares = request.FareClasses.Select(fareClass => CreateFareFromRequest(fareClass, flight)).ToList();
                        }

                        flightsToCreate.Add(flight);
                    }

                    currentFlightNumber = GetNextFlightNumber();
                }

                currentDate = currentDate.AddDays(1);
            }

            if (flightsToCreate.Any())
            {
                _dbContext.Flights.AddRange(flightsToCreate);
                await _dbContext.SaveChangesAsync();
            }

            return flightsToCreate;
        }

        private static Fare CreateFareFromRequest(FareClassRequest request, Flight flight)
        {
            if (!Enum.TryParse<Fare.Fare_class>(request.ClassType, true, out var fareClass))
            {
                throw new ArgumentException($"Неверный тип тарифа: {request.ClassType}");
            }

            return new Fare
            {
                Flight = flight,
                Currency = "RUB",
                Price = request.Price,
                SeatsAvailable = request.Seats,
                BaggageIncluded = !string.IsNullOrWhiteSpace(request.Baggage) && !request.Baggage.Equals("нет", StringComparison.OrdinalIgnoreCase),
                BaggageWeightKg = ParseBaggageWeight(request.Baggage),
                Class = fareClass
            };
        }

        private static int ParseBaggageWeight(string? baggage)
        {
            if (string.IsNullOrWhiteSpace(baggage) || baggage.Equals("нет", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var digits = new string(baggage.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var weight) ? weight : 0;
        }

        public async Task<Flight?> UpdateFlightAsync(int id, Flight updatedFlight, List<FareClassRequest>? fareClasses = null)
        {
            var existingFlight = await _dbContext.Flights
                .Include(f => f.Fares)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (existingFlight == null)
                return null;

            if (existingFlight.Status == FlightStatus.Completed)
                throw new InvalidOperationException("Невозможно редактировать завершённый рейс.");

            var routeError = await ValidateAirportRouteAsync(updatedFlight.OriginAirportId, updatedFlight.DestAirportId);
            if (routeError != null)
                throw new ArgumentException(routeError);

            var previousStatus = existingFlight.Status;
            var changeDescriptions = await GetFlightChangeDescriptionsAsync(existingFlight, updatedFlight);

            existingFlight.FlightNumber = updatedFlight.FlightNumber;
            existingFlight.AirlineId = updatedFlight.AirlineId;
            existingFlight.AircraftId = updatedFlight.AircraftId;
            existingFlight.OriginAirportId = updatedFlight.OriginAirportId;
            existingFlight.DestAirportId = updatedFlight.DestAirportId;
            existingFlight.DepartureDt = updatedFlight.DepartureDt;
            existingFlight.ArrivalDt = updatedFlight.ArrivalDt;
            existingFlight.DurationMinutes = updatedFlight.DurationMinutes;
            existingFlight.Status = updatedFlight.Status;

            if (fareClasses != null)
            {
                var aircraft = await _dbContext.Aircrafts.FindAsync(updatedFlight.AircraftId);
                if (aircraft == null)
                    throw new ArgumentException("Самолёт не найден.");

                var capacity = aircraft.TotalSeats;
                var activeTicketCounts = await _dbContext.Tickets
                    .Where(t => t.FlightId == id && t.Status == TicketStatus.Active)
                    .GroupBy(t => t.FareId)
                    .Select(g => new { FareId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var activeTicketCountsByFare = activeTicketCounts.ToDictionary(x => x.FareId, x => x.Count);
                var requestedFares = new List<(Fare.Fare_class FareClass, FareClassRequest Request)>();

                foreach (var fareClass in fareClasses)
                {
                    if (!Enum.TryParse<Fare.Fare_class>(fareClass.ClassType, true, out var parsedClass))
                    {
                        throw new ArgumentException($"Неверный тип тарифа: {fareClass.ClassType}");
                    }

                    requestedFares.Add((parsedClass, fareClass));
                }

                var totalAllocatedSeats = 0;
                foreach (var (fareClass, requestFare) in requestedFares)
                {
                    if (requestFare.Seats < 0)
                        throw new ArgumentException($"Количество мест для тарифа «{requestFare.Name}» не может быть меньше 0.");

                    if (requestFare.Price < 0)
                        throw new ArgumentException($"Цена тарифа «{requestFare.Name}» должна быть положительной.");

                    totalAllocatedSeats += requestFare.Seats;

                    var existingFare = existingFlight.Fares.FirstOrDefault(f => f.Class == fareClass);
                    if (existingFare != null)
                    {
                        totalAllocatedSeats += activeTicketCountsByFare.GetValueOrDefault(existingFare.Id);
                    }
                }

                if (capacity > 0 && totalAllocatedSeats > capacity)
                {
                    throw new ArgumentException($"Суммарное количество мест тарифов и проданных билетов ({totalAllocatedSeats}) превышает вместимость самолёта ({capacity}).");
                }

                var requestedClassSet = requestedFares.Select(x => x.FareClass).ToHashSet();
                foreach (var existingFare in existingFlight.Fares.ToList())
                {
                    if (!requestedClassSet.Contains(existingFare.Class))
                    {
                        var activeCount = activeTicketCountsByFare.GetValueOrDefault(existingFare.Id);
                        if (activeCount > 0)
                        {
                            throw new InvalidOperationException($"Нельзя удалить тариф «{existingFare.Class}»: по нему уже есть активные билеты.");
                        }

                        _dbContext.Fares.Remove(existingFare);
                    }
                }

                foreach (var (fareClass, requestFare) in requestedFares)
                {
                    var existingFare = existingFlight.Fares.FirstOrDefault(f => f.Class == fareClass);
                    if (existingFare != null)
                    {
                        existingFare.Price = requestFare.Price;
                        existingFare.SeatsAvailable = requestFare.Seats;
                        existingFare.BaggageIncluded = !string.IsNullOrWhiteSpace(requestFare.Baggage) && !requestFare.Baggage.Equals("нет", StringComparison.OrdinalIgnoreCase);
                        existingFare.BaggageWeightKg = ParseBaggageWeight(requestFare.Baggage);
                    }
                    else
                    {
                        existingFlight.Fares.Add(CreateFareFromRequest(requestFare, existingFlight));
                    }
                }
            }

            if (updatedFlight.Status == FlightStatus.Cancelled)
            {
                var activeTickets = await _dbContext.Tickets
                    .Where(t => t.FlightId == id && t.Status == TicketStatus.Active)
                    .ToListAsync();

                foreach (var ticket in activeTickets)
                {
                    ticket.Status = TicketStatus.Cancelled;
                    ticket.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
            }
            else if (updatedFlight.Status == FlightStatus.Completed)
            {
                var activeTickets = await _dbContext.Tickets
                    .Where(t => t.FlightId == id && t.Status == TicketStatus.Active)
                    .ToListAsync();

                foreach (var ticket in activeTickets)
                {
                    ticket.Status = TicketStatus.Used;
                    ticket.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                return existingFlight;
            }
            else if (previousStatus == FlightStatus.Cancelled &&
                     (updatedFlight.Status == FlightStatus.Scheduled || updatedFlight.Status == FlightStatus.Delayed))
            {
                var cancelledTickets = await _dbContext.Tickets
                    .Where(t => t.FlightId == id && t.Status == TicketStatus.Cancelled)
                    .ToListAsync();

                foreach (var ticket in cancelledTickets)
                {
                    ticket.Status = TicketStatus.Active;
                    ticket.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
            }
            else
            {
                await _dbContext.SaveChangesAsync();
            }

            if ((previousStatus != updatedFlight.Status || changeDescriptions.Any()) && updatedFlight.Status != FlightStatus.Completed)
            {
                var recipientEmails = await _dbContext.Tickets
                    .Where(t => t.FlightId == id)
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.User)
                    .Select(t => t.Booking != null && t.Booking.User != null ? t.Booking.User.Email : null)
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Distinct()
                    .ToListAsync();

                await NotifyTicketHoldersAsync(existingFlight, recipientEmails, changeDescriptions);
            }

            return existingFlight;
        }

        private async Task<List<string>> GetFlightChangeDescriptionsAsync(Flight existingFlight, Flight updatedFlight)
        {
            var descriptions = new List<string>();

            if (!string.Equals(existingFlight.FlightNumber, updatedFlight.FlightNumber, StringComparison.Ordinal))
            {
                descriptions.Add($"номер рейса изменён с {existingFlight.FlightNumber} на {updatedFlight.FlightNumber}");
            }

            if (existingFlight.AirlineId != updatedFlight.AirlineId)
            {
                var oldAirline = await _dbContext.Airlines.FindAsync(existingFlight.AirlineId);
                var newAirline = await _dbContext.Airlines.FindAsync(updatedFlight.AirlineId);
                descriptions.Add($"авиакомпания изменена с {oldAirline?.Name ?? existingFlight.AirlineId.ToString()} на {newAirline?.Name ?? updatedFlight.AirlineId.ToString()}");
            }

            if (existingFlight.AircraftId != updatedFlight.AircraftId)
            {
                var oldAircraft = await _dbContext.Aircrafts.FindAsync(existingFlight.AircraftId);
                var newAircraft = await _dbContext.Aircrafts.FindAsync(updatedFlight.AircraftId);
                descriptions.Add($"самолёт изменён с {oldAircraft?.Model ?? existingFlight.AircraftId.ToString()} на {newAircraft?.Model ?? updatedFlight.AircraftId.ToString()}");
            }

            if (existingFlight.OriginAirportId != updatedFlight.OriginAirportId || existingFlight.DestAirportId != updatedFlight.DestAirportId)
            {
                var oldOrigin = await _dbContext.Airports.FindAsync(existingFlight.OriginAirportId);
                var oldDest = await _dbContext.Airports.FindAsync(existingFlight.DestAirportId);
                var newOrigin = await _dbContext.Airports.FindAsync(updatedFlight.OriginAirportId);
                var newDest = await _dbContext.Airports.FindAsync(updatedFlight.DestAirportId);

                var oldRoute = $"{oldOrigin?.Iata ?? existingFlight.OriginAirportId.ToString()} → {oldDest?.Iata ?? existingFlight.DestAirportId.ToString()}";
                var newRoute = $"{newOrigin?.Iata ?? updatedFlight.OriginAirportId.ToString()} → {newDest?.Iata ?? updatedFlight.DestAirportId.ToString()}";
                descriptions.Add($"маршрут изменён с {oldRoute} на {newRoute}");
            }

            if (existingFlight.DepartureDt != updatedFlight.DepartureDt)
            {
                descriptions.Add($"время вылета изменено с {existingFlight.DepartureDt:yyyy-MM-dd HH:mm} на {updatedFlight.DepartureDt:yyyy-MM-dd HH:mm}");
            }

            if (existingFlight.ArrivalDt != updatedFlight.ArrivalDt)
            {
                descriptions.Add($"время прилёта изменено с {existingFlight.ArrivalDt:yyyy-MM-dd HH:mm} на {updatedFlight.ArrivalDt:yyyy-MM-dd HH:mm}");
            }

            if (existingFlight.DurationMinutes != updatedFlight.DurationMinutes)
            {
                descriptions.Add($"длительность рейса изменена с {existingFlight.DurationMinutes} мин на {updatedFlight.DurationMinutes} мин");
            }

            return descriptions;
        }

        public async Task<DeleteFlightResult> DeleteFlightAsync(int id)
        {
            var flight = await _dbContext.Flights
                .Include(f => f.Fares)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null)
                return new DeleteFlightResult { WasDeleted = false, TicketCount = 0 };

            var ticketCount = await _dbContext.Tickets.CountAsync(t => t.FlightId == id);
            if (ticketCount > 0)
            {
                var tickets = await _dbContext.Tickets
                    .Where(t => t.FlightId == id)
                    .ToListAsync();

                _dbContext.Tickets.RemoveRange(tickets);
            }

            if (ticketCount > 0)
            {
                return new DeleteFlightResult
                {
                    WasDeleted = false,
                    TicketCount = ticketCount
                };
            }

            _dbContext.Flights.Remove(flight);
            await _dbContext.SaveChangesAsync();

            return new DeleteFlightResult
            {
                WasDeleted = true,
                TicketCount = ticketCount
            };
        }

        public async Task<Flight?> CancelFlightAsync(int id)
        {
            var flight = await _dbContext.Flights
                .Include(f => f.Tickets)
                    .ThenInclude(t => t.Booking)
                        .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null)
                return null;

            if (flight.Status == FlightStatus.Completed)
                throw new InvalidOperationException("Невозможно отменить завершённый рейс.");

            if (flight.Status == FlightStatus.Cancelled)
                return flight;

            flight.Status = FlightStatus.Cancelled;

            var activeTickets = flight.Tickets
                .Where(t => t.Status == TicketStatus.Active)
                .ToList();

            foreach (var ticket in activeTickets)
            {
                ticket.Status = TicketStatus.Cancelled;
                ticket.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            var recipientEmails = flight.Tickets
                .Select(t => t.Booking != null && t.Booking.User != null ? t.Booking.User.Email : null)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct()
                .ToList();

            await NotifyTicketHoldersAsync(flight, recipientEmails);
            return flight;
        }

        private async Task NotifyTicketHoldersAsync(Flight flight, IEnumerable<string> recipientEmails, IReadOnlyCollection<string>? changeDescriptions = null)
        {
            if (_emailService == null)
                return;

            if (flight.Status == FlightStatus.Completed)
                return;

            var statusDescription = flight.Status.ToString();
            var hasChanges = changeDescriptions != null && changeDescriptions.Any();
            var subject = hasChanges
                ? $"Изменения в рейсе {flight.FlightNumber}"
                : $"Статус рейса {flight.FlightNumber} изменён";

            var body = $"<p>Здравствуйте!</p>";

            if (hasChanges)
            {
                body += "<p>Обратите внимание, что в вашем рейсе были изменены следующие детали:</p>";
                body += "<ul>";
                foreach (var description in changeDescriptions!)
                {
                    body += $"<li>{WebUtility.HtmlEncode(description)}</li>";
                }
                body += "</ul>";
                body += $"<p>Текущий статус рейса: <strong>{WebUtility.HtmlEncode(statusDescription)}</strong>.</p>";
            }
            else
            {
                body += $"<p>Статус вашего рейса <strong>{flight.FlightNumber}</strong> изменён на <strong>{WebUtility.HtmlEncode(statusDescription)}</strong>.</p>";
            }

            body += $"<p>Дата вылета: {flight.DepartureDt:yyyy-MM-dd HH:mm}</p>";
            body += $"<p>Дата прилёта: {flight.ArrivalDt:yyyy-MM-dd HH:mm}</p>";
            body += "<p>Пожалуйста, проверьте детали рейса в личном кабинете или свяжитесь с поддержкой при необходимости.</p>";
            body += "<p>С уважением,<br/>ZetTechAvio</p>";

            foreach (var email in recipientEmails.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                try
                {
                    await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
                }
                catch
                {
                    // Ошибки логируются в EmailService.
                }
            }
        }

        public async Task<List<Fare>> GetFlightFaresAsync(int flightId)
        {
            var fares = await _dbContext.Fares
                .Where(f => f.FlightId == flightId)
                .ToListAsync();
            return fares;
        }

        public async Task<int> MarkPastFlightsCompletedAsync()
        {
            var now = GetMoscowNow();
            var flightsToComplete = await _dbContext.Flights
                .Where(f => f.Status != FlightStatus.Completed && f.Status != FlightStatus.Cancelled && f.ArrivalDt <= now)
                .ToListAsync();

            if (!flightsToComplete.Any())
            {
                return 0;
            }

            foreach (var flight in flightsToComplete)
            {
                flight.Status = FlightStatus.Completed;
            }

            await _dbContext.SaveChangesAsync();
            return flightsToComplete.Count;
        }

        private static DateTime GetMoscowNow()
        {
            try
            {
                var moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.UtcNow.AddHours(3);
            }
            catch (InvalidTimeZoneException)
            {
                return DateTime.UtcNow.AddHours(3);
            }
        }

        public async Task<int> GetFlightTicketCountAsync(int flightId)
        {
            return await _dbContext.Tickets.CountAsync(t => t.FlightId == flightId);
        }

        public async Task<List<FlightTicketResponse>> GetFlightTicketsAsync(int flightId)
        {
            return await _dbContext.Tickets
                .Where(t => t.FlightId == flightId)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.User)
                .Select(t => new FlightTicketResponse
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    PassengerName = t.PassengerName,
                    PassengerType = t.PassengerType.ToString(),
                    Status = t.Status.ToString(),
                    Email = t.Booking != null && t.Booking.User != null ? t.Booking.User.Email : string.Empty,
                    FareId = t.FareId
                })
                .ToListAsync();
        }
    }
}