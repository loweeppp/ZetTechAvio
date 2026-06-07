using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
        Task<List<Flight>> CreateScheduledFlightsAsync(FlightScheduleRequest request);
    }

    public class FlightsService : IFlightsService
    {
        private readonly ApplicationDbContext _dbContext;

        public FlightsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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

            if (string.IsNullOrWhiteSpace(request.FlightNumber))
            {
                var airline = await _dbContext.Airlines.FindAsync(request.AirlineId);
                if (airline == null)
                {
                    throw new ArgumentException("Авиакомпания не найдена для генерации номера рейса.");
                }

                request.FlightNumber = await GenerateFlightNumberAsync(airline.IataCode);
            }

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
                        f.FlightNumber == request.FlightNumber && f.DepartureDt == departureDt);

                    if (!duplicate)
                    {
                        var flight = new Flight
                        {
                            FlightNumber = request.FlightNumber,
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
            }

            await _dbContext.SaveChangesAsync();
            return existingFlight;
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
            return flight;
        }

        public async Task<List<Fare>> GetFlightFaresAsync(int flightId)
        {
            var fares = await _dbContext.Fares
                .Where(f => f.FlightId == flightId)
                .ToListAsync();
            return fares;
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
                    Email = t.Booking != null && t.Booking.User != null ? t.Booking.User.Email : string.Empty
                })
                .ToListAsync();
        }
    }
}