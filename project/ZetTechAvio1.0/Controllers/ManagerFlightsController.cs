using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZetTechAvio1._0.Models;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/manager/flights")]
    [Authorize(Roles = "Manager,Admin")]
    public class ManagerFlightsController : ControllerBase
    {
        private readonly IFlightsService _flightsService;

        public ManagerFlightsController(IFlightsService flightsService)
        {
            _flightsService = flightsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFlights()
        {
            var flights = await _flightsService.GetAllFlightsAsync();
            return Ok(flights);
        }

        [HttpGet("references")]
        public async Task<IActionResult> GetReferenceData()
        {
            var airports = await _flightsService.GetAirportsAsync();
            var airlines = await _flightsService.GetAirlinesAsync();
            var aircrafts = await _flightsService.GetAircraftsAsync();

            return Ok(new
            {
                airports,
                airlines,
                aircrafts
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFlight(int id)
        {
            var flight = await _flightsService.GetFlightByIdAsync(id);
            if (flight == null)
                return NotFound(new { message = "Рейс не найден" });

            return Ok(new
            {
                flight.Id,
                flight.FlightNumber,
                flight.AirlineId,
                flight.AircraftId,
                flight.OriginAirportId,
                flight.DestAirportId,
                flight.DepartureDt,
                flight.ArrivalDt,
                flight.DurationMinutes,
                Status = flight.Status.ToString()
            });
        }

        [HttpGet("{id}/ticket-count")]
        public async Task<IActionResult> GetFlightTicketCount(int id)
        {
            var flight = await _flightsService.GetFlightByIdAsync(id);
            if (flight == null)
                return NotFound(new { message = "Рейс не найден" });

            var ticketCount = await _flightsService.GetFlightTicketCountAsync(id);
            return Ok(new { ticketCount });
        }

        [HttpPost]
        public async Task<IActionResult> CreateFlight([FromBody] FlightCommandRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Некорректные данные рейса" });
            }

            if (request.AirlineId <= 0 || request.AircraftId <= 0 || request.OriginAirportId <= 0 || request.DestAirportId <= 0)
            {
                return BadRequest(new { message = "Выберите авиакомпанию, самолёт и оба аэропорта." });
            }

            var routeError = await _flightsService.ValidateAirportRouteAsync(request.OriginAirportId, request.DestAirportId);
            if (routeError != null)
            {
                return BadRequest(new { message = routeError });
            }

            var flight = new Flight
            {
                FlightNumber = request.FlightNumber,
                AirlineId = request.AirlineId,
                AircraftId = request.AircraftId,
                OriginAirportId = request.OriginAirportId,
                DestAirportId = request.DestAirportId,
                DepartureDt = request.DepartureDt,
                ArrivalDt = request.ArrivalDt,
                DurationMinutes = request.DurationMinutes,
                Status = Enum.Parse<FlightStatus>(request.Status ?? "Scheduled")
            };

            try
            {
                var createdFlight = await _flightsService.CreateFlightAsync(flight);
                return CreatedAtAction(nameof(GetFlight), new { id = createdFlight.Id }, createdFlight);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFlight(int id, [FromBody] FlightCommandRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Некорректные данные рейса" });
            }

            if (request.AirlineId <= 0 || request.AircraftId <= 0 || request.OriginAirportId <= 0 || request.DestAirportId <= 0)
            {
                return BadRequest(new { message = "Выберите авиакомпанию, самолёт и оба аэропорта." });
            }

            var routeError = await _flightsService.ValidateAirportRouteAsync(request.OriginAirportId, request.DestAirportId);
            if (routeError != null)
            {
                return BadRequest(new { message = routeError });
            }

            var updatedFlight = new Flight
            {
                FlightNumber = request.FlightNumber,
                AirlineId = request.AirlineId,
                AircraftId = request.AircraftId,
                OriginAirportId = request.OriginAirportId,
                DestAirportId = request.DestAirportId,
                DepartureDt = request.DepartureDt,
                ArrivalDt = request.ArrivalDt,
                DurationMinutes = request.DurationMinutes,
                Status = Enum.Parse<FlightStatus>(request.Status ?? "Scheduled")
            };

            try
            {
                var flight = await _flightsService.UpdateFlightAsync(id, updatedFlight);
                if (flight == null)
                    return NotFound(new { message = "Рейс не найден" });

                return Ok(flight);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlight(int id)
        {
            try
            {
                var result = await _flightsService.DeleteFlightAsync(id);
                if (!result.WasDeleted)
                    return NotFound(new { message = "Рейс не найден" });

                if (result.TicketCount > 0)
                {
                    return Ok(new
                    {
                        message = $"Рейс удалён вместе с {result.TicketCount} билетами.",
                        ticketCount = result.TicketCount
                    });
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Ошибка удаления рейса" });
            }
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleFlights([FromBody] FlightScheduleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Некорректные данные расписания" });
            }

            var routeError = await _flightsService.ValidateAirportRouteAsync(request.OriginAirportId, request.DestAirportId);
            if (routeError != null)
            {
                return BadRequest(new { message = routeError });
            }

            try
            {
                var createdFlights = await _flightsService.CreateScheduledFlightsAsync(request);
                return Ok(new { created = createdFlights.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
