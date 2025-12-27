using Microsoft.AspNetCore.Mvc;
using ZetTechAvio1._0.Services;


namespace ZetTechAvio1._0.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightsService _flightsService;


        public FlightsController(IFlightsService flightsService)
        {
            _flightsService = flightsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFlights()
        {
            var flights = await _flightsService.GetAllFlightsAsync();

            return Ok(flights);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchFlights([FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? date)
        {
            var flights = await _flightsService.SearchFlightsAsync(from, to, date);
            return Ok(flights);
        }
    }
}