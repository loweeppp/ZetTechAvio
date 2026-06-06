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
        Task<Flight?> UpdateFlightAsync(int id, Flight updatedFlight);
        Task<DeleteFlightResult> DeleteFlightAsync(int id);
        Task<List<Fare>> GetFlightFaresAsync(int flightId);
        Task<List<Airport>> GetAirportsAsync();
        Task<Airport?> GetAirportByIdAsync(int airportId);
        Task<List<Airline>> GetAirlinesAsync();
        Task<Airline?> GetAirlineByIdAsync(int airlineId);
        Task<string?> ValidateAirportRouteAsync(int originAirportId, int destAirportId);
        Task<string> GenerateFlightNumberAsync(string airlinePrefix);
        Task<List<Aircraft>> GetAircraftsAsync();
        Task<int> GetFlightTicketCountAsync(int flightId);
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
                .Include(f => f.Fares)
                .ToListAsync();

            return flights.Select(MapToDto).ToList();
        }
        private FlightDto MapToDto(Flight flight)
        {
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
                    .Include(f => f.Fares);

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

            var sameCityCodes = airports
                .Where(a => !string.IsNullOrWhiteSpace(a.City) && string.Equals(a.City, exactAirport.City, StringComparison.OrdinalIgnoreCase))
                .Select(a => NormalizeAirportCode(a.Iata))
                .Where(code => code != null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return sameCityCodes.Count > 1 ? sameCityCodes : new[] { normalizedInput };
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
                .Select(day => Enum.TryParse<DayOfWeek>(day, true, out var parsed) ? parsed : throw new ArgumentException($"Неверный день недели: {day}"))
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

        public async Task<Flight?> UpdateFlightAsync(int id, Flight updatedFlight)
        {
            var existingFlight = await _dbContext.Flights.FindAsync(id);
            if (existingFlight == null)
                return null;

            var routeError = await ValidateAirportRouteAsync(updatedFlight.OriginAirportId, updatedFlight.DestAirportId);
            if (routeError != null)
                throw new ArgumentException(routeError);

            existingFlight.FlightNumber = updatedFlight.FlightNumber;
            existingFlight.AirlineId = updatedFlight.AirlineId;
            existingFlight.AircraftId = updatedFlight.AircraftId;
            existingFlight.OriginAirportId = updatedFlight.OriginAirportId;
            existingFlight.DestAirportId = updatedFlight.DestAirportId;
            existingFlight.DepartureDt = updatedFlight.DepartureDt;
            existingFlight.ArrivalDt = updatedFlight.ArrivalDt;
            existingFlight.DurationMinutes = updatedFlight.DurationMinutes;
            existingFlight.Status = updatedFlight.Status;

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

            _dbContext.Flights.Remove(flight);
            await _dbContext.SaveChangesAsync();

            return new DeleteFlightResult
            {
                WasDeleted = true,
                TicketCount = ticketCount
            };
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
    }
}