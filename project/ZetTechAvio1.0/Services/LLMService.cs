using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ZetTechAvio1._0.Services
{
    public interface ILLMService
    {
        Task<AiParseResponse> ParseFlightSearchAsync(string text);
    }

    public sealed record AiParseResponse(
        [property: JsonPropertyName("from")] string? From,
        [property: JsonPropertyName("to")] string? To,
        [property: JsonPropertyName("date")] string? Date,
        [property: JsonPropertyName("passengers")] int? Passengers,
        [property: JsonPropertyName("reasoning")] string? Reasoning
    );

    public sealed record AiParseRequest([property: JsonPropertyName("text")] string Text);

    internal sealed record ChatMessage(string role, string content);
    internal sealed record ChatCompletionsRequest(string model, ChatMessage[] messages, double temperature = 0.0, int max_tokens = 200);
    internal sealed record ChatCompletionResponse(Choice[] choices);
    internal sealed record Choice(Message message);
    internal sealed record Message(string role, string content, string? reasoning_content = null);
    internal sealed record ParsedJsonResponse(
        [property: JsonPropertyName("from")] string? from,
        [property: JsonPropertyName("to")] string? to,
        [property: JsonPropertyName("date")] string? date,
        [property: JsonPropertyName("passengers")] int? passengers,
        [property: JsonPropertyName("reasoning")] string? reasoning
    );

    public class LLMService : ILLMService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly string _modelName;

        // Static readonly Regex patterns for performance
        private static readonly Regex DateRegex = new Regex(@"\b(\d{4}-\d{2}-\d{2})\b", RegexOptions.Compiled);
        private static readonly Regex PassengerNumberRegex = new Regex(@"(\d+)\s*(чел|пасс|человек|пассажир)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PassengerPhrasesRegex = new Regex(@"\b(двоих|двух|вдвоем|вдвоём|на двоих)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PassengerTripleRegex = new Regex(@"\b(троих|трех|трёх|на троих)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ReasoningRegex = new Regex(@"reasoning\s*[:\-]\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RussianTextRegex = new Regex(@"[а-яА-ЯёЁ]", RegexOptions.Compiled);

        // Static readonly city data
        private static readonly string[] SupportedCityCodes = { "MOW", "LED", "KZN", "AER", "IST", "DXB", "LON", "PAR", "BKK", "NYC" };
        private static readonly (string Code, string[] Names)[] KnownCities = new[]
        {
            ("MOW", new[] { "москв", "moscow" }),
            ("LED", new[] { "петербург", "санкт-петербург", "питер", "спб", "petersburg" }),
            ("KZN", new[] { "казань", "kazan" }),
            ("AER", new[] { "сочи", "sochi" }),
            ("IST", new[] { "стамбул", "istanbul" }),
            ("DXB", new[] { "дубай", "dubai" }),
            ("LON", new[] { "лондон", "london" }),
            ("PAR", new[] { "париж", "paris" }),
            ("BKK", new[] { "бангкок", "bangkok" }),
            ("NYC", new[] { "нью-йорк", "нью йорк", "new york" })
        };

        public LLMService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["LLM:ApiKey"] ?? configuration["OpenAI:ApiKey"] ?? "lm-studio";
            _modelName = configuration["LLM:Model"] ?? "qwen/qwen3.5-9b";
        }

        public async Task<AiParseResponse> ParseFlightSearchAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text must be provided", nameof(text));

            var client = _httpClientFactory.CreateClient("OpenAI");
            
            if (client.BaseAddress == null)
                throw new InvalidOperationException("HttpClient BaseAddress is not configured.");
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var messages = new[]
            {
                new ChatMessage("system", "Вы — парсер запросов на поиск авиабилетов. Преобразуйте пользовательский запрос в корректный JSON, используя только поддерживаемые коды городов."),
                new ChatMessage("system", "Поддерживаемые коды и города: MOW (Москва), LED (Санкт-Петербург), KZN (Казань), AER (Сочи), IST (Стамбул), DXB (Дубай), LON (Лондон), PAR (Париж), BKK (Бангкок), NYC (Нью-Йорк)."),
                new ChatMessage("system", "Ответ должен быть только JSON-объектом с ключами: from, to, date, passengers, reasoning. Никаких пояснений, markdown, кавычек или текста вне JSON."),
                new ChatMessage("system", "from и to должны быть поддерживаемыми IATA-кодами или null. date должен быть в формате YYYY-MM-DD или null, если не указан. passengers должен быть числом от 1 до 9 или null, если не указан. reasoning должен быть коротким пояснением на русском языке."),
                new ChatMessage("system", "Не используйте reasoning_content для окончательного ответа. Отвечайте только JSON-объектом в поле content."),
                new ChatMessage("user", $"Разберите запрос пользователя:\n{text}")
            };

            var request = new ChatCompletionsRequest(_modelName, messages, temperature: 0.0, max_tokens: 200);
            var endpoint = new Uri(client.BaseAddress, "/v1/chat/completions");
            using var response = await client.PostAsJsonAsync(endpoint, request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"LLM request failed: {response.StatusCode} - {responseBody}");
            }

            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (completion?.choices == null || completion.choices.Length == 0)
                throw new InvalidOperationException($"LLM response is empty. Response body: {responseBody}");

            var message = completion.choices[0]?.message;
            var messageContent = message?.content;
            var reasoningContent = message?.reasoning_content;
            var outputText = messageContent ?? string.Empty;

            if (string.IsNullOrWhiteSpace(outputText))
            {
                var fallback = TryParseFallbackResponse(text);
                if (fallback != null)
                    return fallback;

                if (!string.IsNullOrWhiteSpace(reasoningContent))
                {
                    fallback = TryParseFallbackResponse(reasoningContent);
                    if (fallback != null)
                        return fallback;
                }

                throw new InvalidOperationException($"LLM returned empty content. Response body: {responseBody}");
            }

            if (!TryExtractJson(outputText, out var jsonText))
            {
                var fallback = TryParseFallbackResponse(text);
                if (fallback != null)
                    return fallback;

                if (!string.IsNullOrWhiteSpace(reasoningContent))
                {
                    fallback = TryParseFallbackResponse(reasoningContent);
                    if (fallback != null)
                        return fallback;
                }

                throw new InvalidOperationException($"LLM output does not contain JSON object. Response body: {responseBody}");
            }

            using var document = JsonDocument.Parse(jsonText);
            var root = document.RootElement;

            root.TryGetProperty("from", out var fromElement);
            root.TryGetProperty("to", out var toElement);
            root.TryGetProperty("date", out var dateElement);

            var fromValue = fromElement.ValueKind != JsonValueKind.Null && fromElement.ValueKind != JsonValueKind.Undefined
                ? fromElement.GetString()?.Trim()
                : null;
            var toValue = toElement.ValueKind != JsonValueKind.Null && toElement.ValueKind != JsonValueKind.Undefined
                ? toElement.GetString()?.Trim()
                : null;
            var dateValue = dateElement.ValueKind != JsonValueKind.Null && dateElement.ValueKind != JsonValueKind.Undefined
                ? dateElement.GetString()?.Trim()
                : null;

            var reasoningValue = root.TryGetProperty("reasoning", out var reasoningElement)
                ? reasoningElement.GetString()?.Trim()
                : null;

            if (!IsRussianText(reasoningValue))
            {
                reasoningValue = null;
            }

            var passengersValue = 1;
            if (root.TryGetProperty("passengers", out var passengersElement))
            {
                if (passengersElement.ValueKind == JsonValueKind.Number && passengersElement.TryGetInt32(out var parsedPassengers))
                {
                    passengersValue = parsedPassengers;
                }
                else if (passengersElement.ValueKind == JsonValueKind.String && int.TryParse(passengersElement.GetString(), out var parsedPassengersString))
                {
                    passengersValue = parsedPassengersString;
                }
            }

            passengersValue = Math.Clamp(passengersValue, 1, 9);

            if (string.IsNullOrWhiteSpace(fromValue) && string.IsNullOrWhiteSpace(toValue) && string.IsNullOrWhiteSpace(dateValue))
                throw new InvalidOperationException("LLM returned invalid parse result.");

            return new AiParseResponse(
                fromValue,
                toValue,
                !string.IsNullOrWhiteSpace(dateValue) ? dateValue : null,
                passengersValue,
                string.IsNullOrWhiteSpace(reasoningValue)
                    ? BuildReasoning(fromValue, toValue, dateValue, passengersValue)
                    : reasoningValue
            );
        }

        private static string BuildReasoning(string? from, string? to, string? date, int passengers)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to))
            {
                parts.Add($"маршрут {from} → {to}");
            }
            else if (!string.IsNullOrWhiteSpace(from))
            {
                parts.Add($"отправление из {from}");
            }
            else if (!string.IsNullOrWhiteSpace(to))
            {
                parts.Add($"прибытие в {to}");
            }

            if (!string.IsNullOrWhiteSpace(date))
            {
                parts.Add($"дата {date}");
            }

            parts.Add($"{passengers} пассажир{(passengers == 1 ? string.Empty : passengers < 5 ? "а" : "ов")}");
            return parts.Count > 0 ? $"Распознан запрос: {string.Join(", ", parts)}." : "Распознан запрос.";
        }

        private static bool IsRussianText(string? text)
        {
            return !string.IsNullOrWhiteSpace(text) && RussianTextRegex.IsMatch(text);
        }

        private static bool TryExtractJson(string text, out string jsonText)
        {
            jsonText = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var start = text.IndexOf('{');
            if (start < 0)
                return false;

            var depth = 0;
            for (var i = start; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        jsonText = text[start..(i + 1)];
                        return true;
                    }
                }
            }

            return false;
        }

        private static AiParseResponse? TryParseFallbackResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var normalized = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            var fromCode = ExtractIataCode(normalized, "from") ?? ExtractIataCode(normalized, "из") ?? ExtractCityCodeByName(normalized) ?? ExtractAnySupportedCode(normalized);
            var toCode = ExtractIataCode(normalized, "to") ?? ExtractIataCode(normalized, "в") ?? ExtractCityCodeByName(normalized, skipCode: fromCode) ?? ExtractAnySupportedCode(normalized, skipCode: fromCode);
            var dateValue = ExtractDate(normalized);
            var passengersValue = ExtractPassengerCount(normalized);
            var reasoningValue = ExtractReasoning(normalized);

            if (string.IsNullOrWhiteSpace(fromCode) && string.IsNullOrWhiteSpace(toCode) && string.IsNullOrWhiteSpace(dateValue))
                return null;

            return new AiParseResponse(
                string.IsNullOrWhiteSpace(fromCode) ? null : fromCode,
                string.IsNullOrWhiteSpace(toCode) ? null : toCode,
                string.IsNullOrWhiteSpace(dateValue) ? null : dateValue,
                passengersValue,
                reasoningValue ?? BuildReasoning(fromCode, toCode, dateValue, passengersValue)
            );
        }

        private static string? ExtractIataCode(string text, string label)
        {
            foreach (var code in SupportedCityCodes)
            {
                var pattern = $"\\b{Regex.Escape(label)}\\s*[:\\-]?\\s*{Regex.Escape(code)}\\b";
                if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
                    return code;
            }

            return null;
        }

        private static string? ExtractAnySupportedCode(string text, string? skipCode = null)
        {
            var found = SupportedCityCodes
                .Where(code => !string.Equals(code, skipCode, StringComparison.OrdinalIgnoreCase) &&
                               Regex.IsMatch(text, $"\\b{Regex.Escape(code)}\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();

            return found.Count == 1 ? found[0] : null;
        }

        private static string? ExtractDate(string text)
        {
            var match = DateRegex.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string? ExtractCityCodeByName(string text, string? skipCode = null)
        {
            var lower = text.ToLowerInvariant();
            foreach (var (code, names) in KnownCities)
            {
                if (!string.Equals(code, skipCode, StringComparison.OrdinalIgnoreCase) &&
                    names.Any(name => lower.Contains(name)))
                {
                    return code;
                }
            }

            return null;
        }

        private static int ExtractPassengerCount(string text)
        {
            var match = PassengerNumberRegex.Match(text);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                return Math.Clamp(count, 1, 9);

            if (PassengerPhrasesRegex.IsMatch(text))
                return 2;
            if (PassengerTripleRegex.IsMatch(text))
                return 3;

            return 1;
        }

        private static string? ExtractReasoning(string text)
        {
            var match = ReasoningRegex.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}
