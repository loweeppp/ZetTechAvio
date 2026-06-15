using System;
using System.Collections.Generic;
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

        [Fact]
        public async Task UpdateFlightAsync_WhenFlightIsCancelled_AlsoCancelsActiveTickets()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var origin = new Airport { Id = 1, Iata = "DME", Name = "Домодедово", City = "Москва", Country = "Россия" };
            var dest = new Airport { Id = 2, Iata = "JFK", Name = "Джон Ф. Кеннеди", City = "Нью-Йорк", Country = "США" };
            var airline = new Airline { Id = 1, IataCode = "SU", Name = "Test Airline" };
            var aircraft = new Aircraft { Id = 1, Manufacturer = "Boeing", Model = "737", TotalSeats = 180 };
            var booking = new Booking
            {
                UserId = 1,
                BookingReference = "REF1234567",
                TotalAmount = 10000m,
                Status = BookingStatus.Confirmed
            };

            var flight = new Flight
            {
                FlightNumber = "SU200",
                AirlineId = airline.Id,
                Airline = airline,
                AircraftId = aircraft.Id,
                Aircraft = aircraft,
                OriginAirportId = origin.Id,
                OriginAirport = origin,
                DestAirportId = dest.Id,
                DestAirport = dest,
                DepartureDt = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc),
                ArrivalDt = new DateTime(2026, 7, 31, 18, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 360,
                Status = FlightStatus.Scheduled
            };

            var fare = new Fare
            {
                Flight = flight,
                Currency = "RUB",
                Price = 10000m,
                SeatsAvailable = 10,
                BaggageIncluded = false,
                BaggageWeightKg = 0,
                Refundable = false,
                Class = Fare.Fare_class.Economy
            };

            var ticket = new Ticket
            {
                Booking = booking,
                Flight = flight,
                Fare = fare,
                TicketNumber = "TICK1234567",
                PassengerName = "Иванов Иван",
                Price = 10000m,
                Status = TicketStatus.Active
            };

            context.Airports.AddRange(origin, dest);
            context.Airlines.Add(airline);
            context.Aircrafts.Add(aircraft);
            context.Flights.Add(flight);
            context.Fares.Add(fare);
            context.Bookings.Add(booking);
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var flightsService = new FlightsService(context);

            var updateToCancelled = new Flight
            {
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                DepartureDt = flight.DepartureDt,
                ArrivalDt = flight.ArrivalDt,
                DurationMinutes = flight.DurationMinutes,
                Status = FlightStatus.Cancelled
            };

            var cancelledResult = await flightsService.UpdateFlightAsync(flight.Id, updateToCancelled);

            Assert.NotNull(cancelledResult);
            Assert.Equal(FlightStatus.Cancelled, cancelledResult.Status);
            Assert.Single(cancelledResult.Tickets);
            Assert.Equal(TicketStatus.Cancelled, cancelledResult.Tickets.Single().Status);

            var ticketAfterCancel = await context.Tickets.FirstAsync(t => t.Id == ticket.Id);
            Assert.Equal(TicketStatus.Cancelled, ticketAfterCancel.Status);

            var updateToDelayed = new Flight
            {
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                DepartureDt = flight.DepartureDt,
                ArrivalDt = flight.ArrivalDt,
                DurationMinutes = flight.DurationMinutes,
                Status = FlightStatus.Delayed
            };

            var delayedResult = await flightsService.UpdateFlightAsync(flight.Id, updateToDelayed);

            Assert.NotNull(delayedResult);
            Assert.Equal(FlightStatus.Delayed, delayedResult.Status);
            Assert.Single(delayedResult.Tickets);
            Assert.Equal(TicketStatus.Active, delayedResult.Tickets.Single().Status);

            var ticketAfterDelay = await context.Tickets.FirstAsync(t => t.Id == ticket.Id);
            Assert.Equal(TicketStatus.Active, ticketAfterDelay.Status);

            var updateToCompleted = new Flight
            {
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                DepartureDt = flight.DepartureDt,
                ArrivalDt = flight.ArrivalDt,
                DurationMinutes = flight.DurationMinutes,
                Status = FlightStatus.Completed
            };

            var completedResult = await flightsService.UpdateFlightAsync(flight.Id, updateToCompleted);

            Assert.NotNull(completedResult);
            Assert.Equal(FlightStatus.Completed, completedResult.Status);
            Assert.Single(completedResult.Tickets);
            Assert.Equal(TicketStatus.Used, completedResult.Tickets.Single().Status);

            var ticketAfterComplete = await context.Tickets.FirstAsync(t => t.Id == ticket.Id);
            Assert.Equal(TicketStatus.Used, ticketAfterComplete.Status);

            var updateToScheduled = new Flight
            {
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                DepartureDt = flight.DepartureDt,
                ArrivalDt = flight.ArrivalDt,
                DurationMinutes = flight.DurationMinutes,
                Status = FlightStatus.Scheduled
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => flightsService.UpdateFlightAsync(flight.Id, updateToScheduled));

            var ticketAfterScheduleAttempt = await context.Tickets.FirstAsync(t => t.Id == ticket.Id);
            Assert.Equal(TicketStatus.Used, ticketAfterScheduleAttempt.Status);
        }

        [Fact]
        public async Task UpdateFlightAsync_WhenFlightIsCancelledAgain_StillCancelsActiveTickets()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var origin = new Airport { Id = 1, Iata = "DME", Name = "Домодедово", City = "Москва", Country = "Россия" };
            var dest = new Airport { Id = 2, Iata = "JFK", Name = "Джон Ф. Кеннеди", City = "Нью-Йорк", Country = "США" };
            var airline = new Airline { Id = 1, IataCode = "SU", Name = "Test Airline" };
            var aircraft = new Aircraft { Id = 1, Manufacturer = "Boeing", Model = "737", TotalSeats = 180 };
            var booking = new Booking
            {
                UserId = 1,
                BookingReference = "REF1234567",
                TotalAmount = 10000m,
                Status = BookingStatus.Confirmed
            };

            var flight = new Flight
            {
                FlightNumber = "SU201",
                AirlineId = airline.Id,
                Airline = airline,
                AircraftId = aircraft.Id,
                Aircraft = aircraft,
                OriginAirportId = origin.Id,
                OriginAirport = origin,
                DestAirportId = dest.Id,
                DestAirport = dest,
                DepartureDt = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc),
                ArrivalDt = new DateTime(2026, 7, 31, 18, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 360,
                Status = FlightStatus.Cancelled
            };

            var fare = new Fare
            {
                Flight = flight,
                Currency = "RUB",
                Price = 10000m,
                SeatsAvailable = 10,
                BaggageIncluded = false,
                BaggageWeightKg = 0,
                Refundable = false,
                Class = Fare.Fare_class.Economy
            };

            var ticket = new Ticket
            {
                Booking = booking,
                Flight = flight,
                Fare = fare,
                TicketNumber = "TICK2010001",
                PassengerName = "Петров Петр",
                Price = 10000m,
                Status = TicketStatus.Active
            };

            context.Airports.AddRange(origin, dest);
            context.Airlines.Add(airline);
            context.Aircrafts.Add(aircraft);
            context.Flights.Add(flight);
            context.Fares.Add(fare);
            context.Bookings.Add(booking);
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var flightsService = new FlightsService(context);
            var updateToCancelledAgain = new Flight
            {
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                DepartureDt = flight.DepartureDt,
                ArrivalDt = flight.ArrivalDt,
                DurationMinutes = flight.DurationMinutes,
                Status = FlightStatus.Cancelled
            };

            var cancelledResult = await flightsService.UpdateFlightAsync(flight.Id, updateToCancelledAgain);

            Assert.NotNull(cancelledResult);
            Assert.Equal(FlightStatus.Cancelled, cancelledResult.Status);
            Assert.Single(cancelledResult.Tickets);
            Assert.Equal(TicketStatus.Cancelled, cancelledResult.Tickets.Single().Status);

            var ticketAfterCancelAgain = await context.Tickets.FirstAsync(t => t.Id == ticket.Id);
            Assert.Equal(TicketStatus.Cancelled, ticketAfterCancelAgain.Status);
        }

        [Fact]
        public async Task UpdateFlightAsync_WhenFlightCompletes_MarksActiveTicketsUsed()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var origin = new Airport { Id = 1, Iata = "DME", Name = "Домодедово", City = "Москва", Country = "Россия" };
            var dest = new Airport { Id = 2, Iata = "JFK", Name = "Джон Ф. Кеннеди", City = "Нью-Йорк", Country = "США" };
            var airline = new Airline { Id = 1, IataCode = "SU", Name = "Test Airline" };
            var aircraft = new Aircraft { Id = 1, Manufacturer = "Boeing", Model = "737", TotalSeats = 180 };
            var booking = new Booking
            {
                UserId = 1,
                BookingReference = "REF1234567",
                TotalAmount = 10000m,
                Status = BookingStatus.Confirmed
            };

            var flight = new Flight
            {
                FlightNumber = "SU500",
                AirlineId = airline.Id,
                Airline = airline,
                AircraftId = aircraft.Id,
                Aircraft = aircraft,
                OriginAirportId = origin.Id,
                OriginAirport = origin,
                DestAirportId = dest.Id,
                DestAirport = dest,
                DepartureDt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                ArrivalDt = new DateTime(2026, 8, 1, 14, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 240,
                Status = FlightStatus.Scheduled
            };

            var fare = new Fare
            {
                Flight = flight,
                Currency = "RUB",
                Price = 10000m,
                SeatsAvailable = 10,
                BaggageIncluded = false,
                BaggageWeightKg = 0,
                Refundable = false,
                Class = Fare.Fare_class.Economy
            };

            var ticket = new Ticket
            {
                Booking = booking,
                Flight = flight,
                Fare = fare,
                TicketNumber = "TICK500001",
                PassengerName = "Петров Петр",
                PassengerType = PassengerType.Adult,
                Price = 10000m,
                Status = TicketStatus.Active
            };

            context.Airports.AddRange(origin, dest);
            context.Airlines.Add(airline);
            context.Aircrafts.Add(aircraft);
            context.Flights.Add(flight);
            context.Fares.Add(fare);
            context.Bookings.Add(booking);
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var flightsService = new FlightsService(context);
            var updateToCompleted = new Flight
            {
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                DepartureDt = flight.DepartureDt,
                ArrivalDt = flight.ArrivalDt,
                DurationMinutes = flight.DurationMinutes,
                Status = FlightStatus.Completed
            };

            var completedResult = await flightsService.UpdateFlightAsync(flight.Id, updateToCompleted);

            Assert.NotNull(completedResult);
            Assert.Equal(FlightStatus.Completed, completedResult.Status);
            Assert.Single(completedResult.Tickets);
            Assert.Equal(TicketStatus.Used, completedResult.Tickets.Single().Status);

            var ticketAfterComplete = await context.Tickets.FirstAsync(t => t.Id == ticket.Id);
            Assert.Equal(TicketStatus.Used, ticketAfterComplete.Status);
        }

        [Fact]
        public async Task UpdateFlightAsync_ThrowsWhenEditingCompletedFlight()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var origin = new Airport { Id = 1, Iata = "DME", Name = "Домодедово", City = "Москва", Country = "Россия" };
            var dest = new Airport { Id = 2, Iata = "JFK", Name = "Джон Ф. Кеннеди", City = "Нью-Йорк", Country = "США" };
            var airline = new Airline { Id = 1, IataCode = "SU", Name = "Test Airline" };
            var aircraft = new Aircraft { Id = 1, Manufacturer = "Boeing", Model = "737", TotalSeats = 180 };

            var flight = new Flight
            {
                FlightNumber = "SU300",
                AirlineId = airline.Id,
                Airline = airline,
                AircraftId = aircraft.Id,
                Aircraft = aircraft,
                OriginAirportId = origin.Id,
                OriginAirport = origin,
                DestAirportId = dest.Id,
                DestAirport = dest,
                DepartureDt = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc),
                ArrivalDt = new DateTime(2026, 7, 31, 18, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 360,
                Status = FlightStatus.Completed
            };

            context.Airports.AddRange(origin, dest);
            context.Airlines.Add(airline);
            context.Aircrafts.Add(aircraft);
            context.Flights.Add(flight);
            await context.SaveChangesAsync();

            var flightsService = new FlightsService(context);
            var updatedFlight = new Flight
            {
                FlightNumber = "SU300",
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                DepartureDt = flight.DepartureDt,
                ArrivalDt = flight.ArrivalDt,
                DurationMinutes = flight.DurationMinutes,
                Status = FlightStatus.Delayed
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await flightsService.UpdateFlightAsync(flight.Id, updatedFlight));
        }

        [Fact]
        public async Task GetFlightTicketsAsync_ReturnsTicketListWithCustomerEmail()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var origin = new Airport { Id = 1, Iata = "DME", Name = "Домодедово", City = "Москва", Country = "Россия" };
            var dest = new Airport { Id = 2, Iata = "JFK", Name = "Джон Ф. Кеннеди", City = "Нью-Йорк", Country = "США" };
            var airline = new Airline { Id = 1, IataCode = "SU", Name = "Test Airline" };
            var aircraft = new Aircraft { Id = 1, Manufacturer = "Boeing", Model = "737", TotalSeats = 180 };
            var user = new User { Id = 1, Email = "test@example.com", PasswordHash = "hash", FullName = "Тест Тестов", Phone = "1234567890", Role = UserRole.User };
            var booking = new Booking
            {
                UserId = user.Id,
                User = user,
                BookingReference = "REF1234567",
                TotalAmount = 10000m,
                Status = BookingStatus.Paid
            };

            var flight = new Flight
            {
                FlightNumber = "SU400",
                AirlineId = airline.Id,
                Airline = airline,
                AircraftId = aircraft.Id,
                Aircraft = aircraft,
                OriginAirportId = origin.Id,
                OriginAirport = origin,
                DestAirportId = dest.Id,
                DestAirport = dest,
                DepartureDt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                ArrivalDt = new DateTime(2026, 8, 1, 14, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 240,
                Status = FlightStatus.Scheduled
            };

            var fare = new Fare
            {
                Flight = flight,
                Currency = "RUB",
                Price = 10000m,
                SeatsAvailable = 10,
                BaggageIncluded = false,
                BaggageWeightKg = 0,
                Refundable = false,
                Class = Fare.Fare_class.Economy
            };

            var ticket = new Ticket
            {
                Booking = booking,
                Flight = flight,
                Fare = fare,
                TicketNumber = "TICK123456",
                PassengerName = "Иванов Иван",
                PassengerType = PassengerType.Adult,
                Price = 10000m,
                Status = TicketStatus.Active
            };

            context.Users.Add(user);
            context.Airports.AddRange(origin, dest);
            context.Airlines.Add(airline);
            context.Aircrafts.Add(aircraft);
            context.Flights.Add(flight);
            context.Fares.Add(fare);
            context.Bookings.Add(booking);
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var flightsService = new FlightsService(context);
            var tickets = await flightsService.GetFlightTicketsAsync(flight.Id);

            Assert.Single(tickets);
            Assert.Equal("test@example.com", tickets[0].Email);
            Assert.Equal("TICK123456", tickets[0].TicketNumber);
            Assert.Equal("Иванов Иван", tickets[0].PassengerName);
        }

        [Fact]
        public async Task UpdateFlightAsync_WhenFlightDetailsChange_NotifiesTicketHolders()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var origin = new Airport { Id = 1, Iata = "DME", Name = "Домодедово", City = "Москва", Country = "Россия" };
            var dest = new Airport { Id = 2, Iata = "JFK", Name = "Джон Ф. Кеннеди", City = "Нью-Йорк", Country = "США" };
            var airline = new Airline { Id = 1, IataCode = "SU", Name = "Test Airline" };
            var aircraft = new Aircraft { Id = 1, Manufacturer = "Boeing", Model = "737", TotalSeats = 180 };
            var user = new User { Id = 1, Email = "test@example.com", PasswordHash = "hash", FullName = "Тест Тестов", Phone = "1234567890", Role = UserRole.User };
            var booking = new Booking
            {
                UserId = user.Id,
                User = user,
                BookingReference = "REF1234567",
                TotalAmount = 10000m,
                Status = BookingStatus.Confirmed
            };

            var flight = new Flight
            {
                FlightNumber = "SU600",
                AirlineId = airline.Id,
                Airline = airline,
                AircraftId = aircraft.Id,
                Aircraft = aircraft,
                OriginAirportId = origin.Id,
                OriginAirport = origin,
                DestAirportId = dest.Id,
                DestAirport = dest,
                DepartureDt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                ArrivalDt = new DateTime(2026, 8, 1, 14, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 240,
                Status = FlightStatus.Scheduled
            };

            var fare = new Fare
            {
                Flight = flight,
                Currency = "RUB",
                Price = 10000m,
                SeatsAvailable = 10,
                BaggageIncluded = false,
                BaggageWeightKg = 0,
                Refundable = false,
                Class = Fare.Fare_class.Economy
            };

            var ticket = new Ticket
            {
                Booking = booking,
                Flight = flight,
                Fare = fare,
                TicketNumber = "TICK600001",
                PassengerName = "Иванов Иван",
                PassengerType = PassengerType.Adult,
                Price = 10000m,
                Status = TicketStatus.Active
            };

            context.Airports.AddRange(origin, dest);
            context.Airlines.Add(airline);
            context.Aircrafts.Add(aircraft);
            context.Users.Add(user);
            context.Flights.Add(flight);
            context.Fares.Add(fare);
            context.Bookings.Add(booking);
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var emailService = new FakeEmailService();
            var flightsService = new FlightsService(context, emailService);

            var updatedFlight = new Flight
            {
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                AircraftId = flight.AircraftId,
                OriginAirportId = flight.OriginAirportId,
                DestAirportId = flight.DestAirportId,
                DepartureDt = flight.DepartureDt.AddHours(2),
                ArrivalDt = flight.ArrivalDt.AddHours(2),
                DurationMinutes = flight.DurationMinutes,
                Status = flight.Status
            };

            var result = await flightsService.UpdateFlightAsync(flight.Id, updatedFlight);

            Assert.NotNull(result);
            Assert.Equal(FlightStatus.Scheduled, result.Status);
            Assert.Single(emailService.SentMessages);
            var sentMessage = emailService.SentMessages.Single();
            Assert.Equal("test@example.com", sentMessage.To);
            Assert.Contains("Изменения в рейсе", sentMessage.Subject);
            Assert.Contains("время вылета изменено", sentMessage.Body);
            Assert.Contains("время прилёта изменено", sentMessage.Body);
        }

        private sealed class FakeEmailService : IEmailService
        {
            public List<(string To, string Subject, string Body, bool IsHtml)> SentMessages { get; } = new();

            public Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, IEnumerable<EmailAttachment>? attachments = null)
            {
                SentMessages.Add((to, subject, body, isHtml));
                return Task.FromResult(true);
            }
        }
    }
}
