using ZetTechAvio1._0.Services;
using Microsoft.AspNetCore.Mvc;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaresController : ControllerBase
    {
        private readonly IFaresService _faresService;

        

        public FaresController(IFaresService faresService)
        {
            _faresService = faresService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFares()
        {
            var fares = await _faresService.GetAllFaresAsync();
            return Ok(fares);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFareById(int id)
        {
            var fare = await _faresService.GetFareByIdAsync(id);
            if (fare == null)
                return NotFound(new { message = "Fare not found" });

            return Ok(fare);
        }
    }
}