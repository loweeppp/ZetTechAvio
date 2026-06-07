using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private const int MaxParseRequestLength = 500;

        private readonly ILLMService _llmService;
        private readonly ILogger<AiController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiController(ILLMService llmService, ILogger<AiController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _llmService = llmService;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpPost("parse")]
        public async Task<IActionResult> ParseSearchText([FromBody] AiParseRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { message = "Поле text обязательно." });

            if (request.Text.Length > MaxParseRequestLength)
                return BadRequest(new { message = $"Максимальная длина запроса {MaxParseRequestLength} символов." });

            try
            {
                var result = await _llmService.ParseFlightSearchAsync(request.Text);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM parse error");
                return StatusCode(500, new { message = "Не удалось распознать запрос. Попробуйте переформулировать." });
            }
        }

        [HttpPost("transcribe")]
        public async Task<IActionResult> Transcribe(IFormFile audio)
        {
            try
            {
                if (audio == null || audio.Length == 0)
                    return BadRequest(new { message = "Аудио файл не получен" });

                using var content = new MultipartFormDataContent();
                using var stream = audio.OpenReadStream();
                using var streamContent = new StreamContent(stream);

                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(streamContent, "file", audio.FileName ?? "audio.wav");
                content.Add(new StringContent("ru"), "language");
                content.Add(new StringContent("transcriptions"), "task");

                var whisperUrl = _configuration["Whisper:Url"] ?? "http://localhost:8081";
                var response = await _httpClient.PostAsync($"{whisperUrl}/inference", content);

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Whisper transcription failed: {StatusCode} {ResponseBody}", response.StatusCode, responseBody);
                    return StatusCode(500, new { message = "Ошибка транскрипции", details = responseBody });
                }

                string text = null;
                try
                {
                    using var result = JsonDocument.Parse(responseBody);
                    var root = result.RootElement;

                    if (root.TryGetProperty("text", out var textElement))
                        text = textElement.GetString();
                    else if (root.TryGetProperty("transcription", out textElement))
                        text = textElement.GetString();
                    else if (root.TryGetProperty("output", out textElement))
                        text = textElement.GetString();
                    else if (root.TryGetProperty("result", out var resultElement) && resultElement.ValueKind == JsonValueKind.Object && resultElement.TryGetProperty("text", out textElement))
                        text = textElement.GetString();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Whisper response is not valid JSON: {ResponseBody}", responseBody);
                    return StatusCode(500, new { message = "Ошибка транскрипции", details = "Ответ от Whisper-сервера не является JSON." });
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogError("Whisper transcription returned no text: {ResponseBody}", responseBody);
                    return StatusCode(500, new { message = "Ошибка транскрипции", details = responseBody });
                }

                return Ok(new { text = text.Trim() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transcribe error");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }
    }

    public sealed record AiParseRequest([property: JsonPropertyName("text")] string Text);
}
