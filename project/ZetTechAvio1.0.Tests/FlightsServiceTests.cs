using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Tests
{
    public class FlightsServiceTests
    {
        [Fact]
        public async Task SearchFlightsAsync_UsesDestinationAirportGroup_WhenTargetIataIsMultiAirportCity()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var moscow = new Airport { Id = 1, Iata = "MOW", Name = "Шереметьево", City = "Москва", Country = "Россия" };
            var dme = new Airport { Id = 2, Iata = "DME", Name = "Домодедово", City = "Москва", Country = "Россия" };
            var vko = new Airport { Id = 3, Iata = "VKO", Name = "Внуково", City = "Москва", Country = "Россия" };
            var jfk = new Airport { Id = 4, Iata = "JFK", Name = "Джон Ф. Кеннеди", City = "Нью-Йорк", Country = "США" };
            var lga = new Airport { Id = 5, Iata = "LGA", Name = "Ла-Гуардия", City = "Нью-Йорк", Country = "США" };

            var airline = new Airline { Id = 1, IataCode = "SU", Name = "Test Airline" };
            var aircraft = new Aircraft { Id = 1, Manufacturer = "Boeing", Model = "737", TotalSeats = 180 };

            context.Airports.AddRange(moscow, dme, vko, jfk, lga);
            context.Airlines.Add(airline);
            context.Aircrafts.Add(aircraft);

            context.Flights.AddRange(
                new Flight
                {
                    FlightNumber = "SU100",
                    AirlineId = airline.Id,
                    Airline = airline,
                    AircraftId = aircraft.Id,
                    Aircraft = aircraft,
                    OriginAirportId = dme.Id,
                    OriginAirport = dme,
                    DestAirportId = jfk.Id,
                    DestAirport = jfk,
                    DepartureDt = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc),
                    ArrivalDt = new DateTime(2026, 7, 31, 18, 0, 0, DateTimeKind.Utc),
                    DurationMinutes = 360,
                    Status = FlightStatus.Scheduled
                },
                new Flight
                {
                    FlightNumber = "SU101",
                    AirlineId = airline.Id,
                    Airline = airline,
                    AircraftId = aircraft.Id,
                    Aircraft = aircraft,
                    OriginAirportId = dme.Id,
                    OriginAirport = dme,
                    DestAirportId = lga.Id,
                    DestAirport = lga,
                    DepartureDt = new DateTime(2026, 7, 31, 16, 0, 0, DateTimeKind.Utc),
                    ArrivalDt = new DateTime(2026, 7, 31, 20, 0, 0, DateTimeKind.Utc),
                    DurationMinutes = 240,
                    Status = FlightStatus.Scheduled
                },
                new Flight
                {
                    FlightNumber = "SU102",
                    AirlineId = airline.Id,
                    Airline = airline,
                    AircraftId = aircraft.Id,
                    Aircraft = aircraft,
                    OriginAirportId = dme.Id,
                    OriginAirport = dme,
                    DestAirportId = jfk.Id,
                    DestAirport = jfk,
                    DepartureDt = new DateTime(2026, 7, 31, 18, 0, 0, DateTimeKind.Utc),
                    ArrivalDt = new DateTime(2026, 7, 31, 22, 0, 0, DateTimeKind.Utc),
                    DurationMinutes = 240,
                    Status = FlightStatus.Scheduled
                });

            await context.SaveChangesAsync();

            var flightsService = new FlightsService(context);
            var results = await flightsService.SearchFlightsAsync("DME", "JFK", "2026-07-31");

            Assert.Equal(3, results.Count);
            Assert.Contains(results, flight => flight.DestAirport != null && flight.DestAirport.Iata == "JFK");
            Assert.Contains(results, flight => flight.DestAirport != null && flight.DestAirport.Iata == "LGA");
        }
    }
}
