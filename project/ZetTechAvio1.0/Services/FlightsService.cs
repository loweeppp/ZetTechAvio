using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public interface IFlightsService
    {
        Task<List<Flight>> GetAllFlightsAsync();
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

        public async Task<List<Flight>> GetAllFlightsAsync()
        {
            return await _dbContext.Flights.ToListAsync();
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