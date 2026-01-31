using Microsoft.EntityFrameworkCore;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public interface IFlightsService
    {
        Task<List<FlightDto>> GetAllFlightsAsync();
        Task<List<FlightDto>> SearchFlightsAsync(string from, string to, string date);
        Task<Flight?> GetFlightByIdAsync(int id);
        Task<Flight> CreateFlightAsync(Flight flight);
        Task<Flight?> UpdateFlightAsync(int id, Flight updatedFlight);
        Task<bool> DeleteFlightAsync(int id);
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
                MinPrice = flight.Fares.Any() ? flight.Fares.Min(f => f.Price) : 0,
                OriginAirport = flight.OriginAirport,
                DestAirport = flight.DestAirport
            };
        }
        public async Task<List<FlightDto>> SearchFlightsAsync(string from, string to, string date)
        {
            try
            {
                var query = _dbContext.Flights
                    .Include(f => f.OriginAirport)
                    .Include(f => f.DestAirport)
                    .Include(f => f.Fares);

                var flights = await query.ToListAsync();

                if (!string.IsNullOrEmpty(from))
                {
                    flights = flights.Where(f =>
                        (f.OriginAirport?.Iata?.ToLower().Contains(from.ToLower()) ?? false) ||
                        (f.OriginAirport?.City?.ToLower().Contains(from.ToLower()) ?? false))
                        .ToList();
                }

                if (!string.IsNullOrEmpty(to))
                {
                    flights = flights.Where(f =>
                        (f.DestAirport?.Iata?.ToLower().Contains(to.ToLower()) ?? false) ||
                        (f.DestAirport?.City?.ToLower().Contains(to.ToLower()) ?? false))
                        .ToList();
                }

                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
                {
                    flights = flights.Where(f => f.DepartureDt.Date == parsedDate.Date).ToList();
                }

                return flights.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SearchFlightsAsync error: {ex.Message}");
                return new List<FlightDto>();
            }
        }


        public async Task<Flight?> GetFlightByIdAsync(int id)
        {
            return await _dbContext.Flights.FindAsync(id);
        }

        public async Task<Flight> CreateFlightAsync(Flight flight)
        {
            _dbContext.Flights.Add(flight);
            await _dbContext.SaveChangesAsync();
            return flight;
        }

        public async Task<Flight?> UpdateFlightAsync(int id, Flight updatedFlight)
        {
            var existingFlight = await _dbContext.Flights.FindAsync(id);
            if (existingFlight == null)
                return null;

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

        public async Task<bool> DeleteFlightAsync(int id)
        {
            var flight = await _dbContext.Flights.FindAsync(id);
            if (flight == null)
                return false;

            _dbContext.Flights.Remove(flight);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}