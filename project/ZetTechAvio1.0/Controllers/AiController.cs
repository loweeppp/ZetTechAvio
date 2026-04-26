using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly ILLMService _llmService;
        private readonly ILogger<AiController> _logger;

        public AiController(ILLMService llmService, ILogger<AiController> logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        [HttpPost("parse")]
        public async Task<IActionResult> ParseSearchText([FromBody] AiParseRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { message = "Поле text обязательно." });

            try
            {
                var result = await _llmService.ParseFlightSearchAsync(request.Text);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM parse error");
                return StatusCode(500, new { message = "Не удалось распознать запрос. Попробуйте переформулировать." });
            }
        }
    }

    public sealed record AiParseRequest([property: JsonPropertyName("text")] string Text);
}
