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


    }
}