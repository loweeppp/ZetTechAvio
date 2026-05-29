using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using ZetTechAvio1._0.Models;

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
        [property: JsonPropertyName("dateFrom")] string? DateFrom = null,
        [property: JsonPropertyName("dateTo")] string? DateTo = null,
        [property: JsonPropertyName("passengers")] int? Passengers = null,
        [property: JsonPropertyName("minPrice")] int? MinPrice = null,
        [property: JsonPropertyName("maxPrice")] int? MaxPrice = null,
        [property: JsonPropertyName("maxDurationMinutes")] int? MaxDurationMinutes = null,
        [property: JsonPropertyName("baggageRequired")] bool? BaggageRequired = null,
        [property: JsonPropertyName("suggestAlternative")] bool? SuggestAlternative = null,
        [property: JsonPropertyName("statusMessage")] string? StatusMessage = null,
        [property: JsonPropertyName("reasoning")] string? Reasoning = null
    );

    public sealed record AiParseRequest([property: JsonPropertyName("text")] string Text);

    internal sealed record ChatMessage(string role, string content);
    internal sealed record ChatCompletionsRequest(string model, ChatMessage[] messages, double temperature = 0.0, int max_tokens = 400);
    internal sealed record ChatCompletionResponse(Choice[] choices);
    internal sealed record Choice(Message? message, string? text);
    internal sealed record Message(string role, string content, string? reasoning_content = null);

    public class LLMService : ILLMService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFlightsService _flightsService;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly int _retryDelaySeconds;
        private readonly int _maxTokens;
        private readonly bool _logRawLlmResponse;
        private static readonly Regex StripThink = new(@"<think>[\s\S]*?</think>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public LLMService(IHttpClientFactory httpClientFactory, IFlightsService flightsService, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _flightsService = flightsService;
            _apiKey = configuration["LLM:ApiKey"] ?? configuration["OpenAI:ApiKey"] ?? "lm-studio";
            _modelName = configuration["LLM:Model"] ?? "qwen/qwen3.5-9b";
            _retryDelaySeconds = int.TryParse(configuration["LLM:RetryDelaySeconds"], out var delay)
                ? Math.Max(delay, 1)
                : 5;
            _maxTokens = int.TryParse(configuration["LLM:MaxTokens"], out var maxTokens)
                ? Math.Clamp(maxTokens, 400, 2000)
                : 1200;
            _logRawLlmResponse = bool.TryParse(configuration["LLM:LogRawResponse"], out var logRaw) && logRaw;
        }

        private const int MaxLlmRetries = 2;

        public async Task<AiParseResponse> ParseFlightSearchAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text must be provided", nameof(text));

            var client = _httpClientFactory.CreateClient("OpenAI");
            if (client.BaseAddress == null)
                throw new InvalidOperationException("HttpClient BaseAddress is not configured.");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            var endpoint = new Uri(client.BaseAddress, "/v1/chat/completions");

            var messages = new List<ChatMessage>
            {
                new ChatMessage("system", await BuildSystemPromptAsync()),
                new ChatMessage("user", BuildUserPrompt(text))
            };

            string? lastError = null;
            for (var attempt = 1; attempt <= MaxLlmRetries; attempt++)
            {
                var outputText = await SendChatCompletionRequestAsync(client, endpoint, messages);
                if (TryParseAndValidateResponse(outputText, out var parsedResponse, out var validationError))
                    return parsedResponse!;

                lastError = validationError ?? "LLM returned an invalid response.";
                if (attempt < MaxLlmRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_retryDelaySeconds));
                    messages.Add(new ChatMessage("user", BuildRetryPrompt(lastError, text)));
                }
            }

            throw new InvalidOperationException($"LLM did not return a valid JSON parse after {MaxLlmRetries} attempts: {lastError}");
        }

        private async Task<string> BuildSystemPromptAsync()
        {
            var now = DateTime.UtcNow;
            var airportList = await BuildAirportListAsync();
            var routeSummary = await BuildRouteSummaryAsync();
            var flightSchedule = await BuildFlightScheduleAsync();

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"Вы — агент парсинга запросов ZetTechAvio. Сегодня: {now:yyyy-MM-dd}, текущее время: {now:HH:mm} UTC.");
            promptBuilder.AppendLine("Отвечайте ТОЛЬКО одним JSON-объектом без markdown, без code fence, без пояснений и без текста вне JSON.");
            promptBuilder.AppendLine("Ответ должен начинаться с '{' и заканчиваться '}'.");
            promptBuilder.AppendLine("Если нужно подумать, используйте <think>, но итоговый ответ должен быть валидным JSON.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Список аэропортов, которые доступны в системе:");
            promptBuilder.AppendLine(airportList);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Актуальное расписание доступных рейсов:");
            promptBuilder.AppendLine(flightSchedule);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(routeSummary);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Правила:");
            promptBuilder.AppendLine("1. from — IATA-код аэропорта отправления или null.");
            promptBuilder.AppendLine("2. to — IATA-код аэропорта назначения или null.");
            promptBuilder.AppendLine("3. date — дата вылета в формате YYYY-MM-DD или YYYY-MM-DDTHH:mm, либо null.");
            promptBuilder.AppendLine("4. dateFrom и dateTo — используйте только для диапазонов дат.");
            promptBuilder.AppendLine("5. passengers — целое число от 1 до 5, или 1 если не указано.");
            promptBuilder.AppendLine("6. reasoning — короткое русское пояснение, что было распознано.");
            promptBuilder.AppendLine("7. minPrice, maxPrice, maxDurationMinutes, baggageRequired, suggestAlternative, statusMessage — заполняйте только при явной просьбе.");
            promptBuilder.AppendLine("8. Если значение неизвестно, используйте null.");
            promptBuilder.AppendLine("9. Если город задан русским именем, сопоставьте его с IATA-кодом из списка выше.");
            promptBuilder.AppendLine("10. Если пользователь просит \"ближайшие рейсы\" или не указывает дату, оставьте date=null.");
            promptBuilder.AppendLine("11. Относительные даты (сегодня, завтра, послезавтра, на следующей неделе, следующую пятницу, через 3 дня) разрешайте относительно текущей UTC-даты.");
            promptBuilder.AppendLine($"12. Сегодня UTC-дата: {now:yyyy-MM-dd}.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Если запрос содержит условие вида 'если не получится, то ...', установите suggestAlternative:true и statusMessage с кратким объяснением альтернативы.");
            promptBuilder.AppendLine("Используйте только поля: from, to, date, dateFrom, dateTo, passengers, minPrice, maxPrice, maxDurationMinutes, baggageRequired, suggestAlternative, statusMessage, reasoning.");
            promptBuilder.AppendLine("Пример ответа:");
            promptBuilder.AppendLine("{\"from\":\"DME\",\"to\":\"JFK\",\"date\":null,\"passengers\":1,\"suggestAlternative\":true,\"statusMessage\":\"если Нью-Йорк недоступен, предложить Москву\",\"reasoning\":\"Ближайшие рейсы в Нью-Йорк с альтернативой Москва\"}");

            return promptBuilder.ToString();
        }

        private static string BuildUserPrompt(string text)
        {
            return $"Проанализируй запрос и верни строго один JSON-объект для поиска рейсов. Не добавляй пояснений и не используй markdown. Запрос: {text.Trim()}";
        }

        private async Task<string> BuildAirportListAsync()
        {
            var airports = await _flightsService.GetAirportsAsync();
            if (airports == null || airports.Count == 0)
                return "Доступных аэропортов нет.";

            return string.Join(Environment.NewLine,
                airports
                    .OrderBy(a => a.Iata)
                    .Select(a => $"{a.Iata} → {a.City} ({a.Name})")
                    .Distinct());
        }

        private async Task<string> BuildRouteSummaryAsync()
        {
            var routes = await _flightsService.GetAllFlightsAsync();
            var activeFlights = routes
                .Where(f => !string.IsNullOrWhiteSpace(f.OriginAirport?.Iata) && !string.IsNullOrWhiteSpace(f.DestAirport?.Iata))
                .ToList();

            if (!activeFlights.Any())
                return "Доступных маршрутов нет.";

            var routeGroups = activeFlights
                .GroupBy(f => new { Origin = f.OriginAirport!.Iata, Dest = f.DestAirport!.Iata })
                .Select(g => new { Route = $"{g.Key.Origin}->{g.Key.Dest}", Count = g.Count() })
                .OrderBy(r => r.Route)
                .ToList();

            var summary = string.Join(", ", routeGroups.Take(8).Select(r => $"{r.Route} ({r.Count})"));
            if (routeGroups.Count > 8)
            {
                summary += $", и ещё {routeGroups.Count - 8} маршрутов";
            }

            var minPrice = activeFlights.Min(f => f.MinPrice);
            var maxPrice = activeFlights.Max(f => f.MinPrice);
            var minDuration = activeFlights.Min(f => f.DurationMinutes);
            var maxDuration = activeFlights.Max(f => f.DurationMinutes);
            var earliestDeparture = activeFlights.Min(f => f.DepartureDt).ToString("yyyy-MM-dd");
            var latestDeparture = activeFlights.Max(f => f.DepartureDt).ToString("yyyy-MM-dd");

            return $"Доступные направления: {summary}. Цены: от {minPrice} до {maxPrice} руб. Время в пути: от {minDuration} до {maxDuration} мин. Вылеты в диапазоне {earliestDeparture} — {latestDeparture}.";
        }

        private async Task<string> BuildFlightScheduleAsync()
        {
            var flights = await _flightsService.GetAllFlightsAsync();
            var upcoming = flights
                .Where(f => f.DepartureDt >= DateTime.UtcNow)
                .OrderBy(f => f.DepartureDt)
                .Take(100)
                .ToList();

            if (!upcoming.Any())
                return "Актуальных рейсов нет.";

            return string.Join(Environment.NewLine, upcoming.Select(f =>
                $"{f.OriginAirport.City} ({f.OriginAirport.Iata}) -> {f.DestAirport.City} ({f.DestAirport.Iata}) | {f.DepartureDt:yyyy-MM-dd HH:mm} | цена от {f.MinPrice} руб. | {f.FlightNumber}"));
        }

        private static string BuildRetryPrompt(string error, string originalText)
        {
            return $"Предыдущий ответ был неверен: {error}. Верни строго один JSON-объект, начинающийся с '{{' и заканчивающийся '}}'. Используй только поля from/to/date/dateFrom/dateTo/passengers/minPrice/maxPrice/maxDurationMinutes/baggageRequired/suggestAlternative/statusMessage/reasoning. Запрос: {originalText}";
        }

        private async Task<string> SendChatCompletionRequestAsync(HttpClient client, Uri endpoint, List<ChatMessage> messages)
        {
            var request = new ChatCompletionsRequest(_modelName, messages.ToArray(), temperature: 0.0, max_tokens: _maxTokens);
            using var response = await client.PostAsJsonAsync(endpoint, request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (_logRawLlmResponse)
            {
                Console.WriteLine("LLM raw response:");
                Console.WriteLine(responseBody);
            }

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"LLM request failed: {response.StatusCode} - {responseBody}");

            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (completion?.choices == null || completion.choices.Length == 0)
                throw new InvalidOperationException($"LLM response is empty. Response body: {responseBody}");

            var choice = completion.choices[0];
            var message = choice.message;
            var content = message?.content;
            if (string.IsNullOrWhiteSpace(content))
                content = message?.reasoning_content;
            if (string.IsNullOrWhiteSpace(content))
                content = choice.text;
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException($"LLM response content is empty. Response body: {responseBody}");

            return content.Trim();
        }

        private static bool TryParseAndValidateResponse(string outputText, out AiParseResponse? response, out string? error)
        {
            response = null;
            error = null;

            if (string.IsNullOrWhiteSpace(outputText))
            {
                error = "LLM returned empty content.";
                return false;
            }

            if (!TryExtractJsonObject(outputText, out var jsonText))
            {
                error = "LLM output does not contain a valid JSON object.";
                return false;
            }

            if (!TryParseAndValidateJson(jsonText, out var parsed, out var validationError))
            {
                error = validationError;
                return false;
            }

            response = parsed;
            return true;
        }

        internal static bool TryParseAndValidateJson(string jsonText, out AiParseResponse? response, out string? error)
        {
            response = null;
            error = null;

            try
            {
                using var document = JsonDocument.Parse(jsonText);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    error = "JSON root must be an object.";
                    return false;
                }

                if (!ValidateAllowedProperties(root, out var propertyError))
                {
                    error = propertyError;
                    return false;
                }

                var fromValue = GetOptionalString(root, "from");
                var toValue = GetOptionalString(root, "to");
                var dateValue = GetOptionalString(root, "date");
                var dateFromValue = GetOptionalString(root, "dateFrom");
                var dateToValue = GetOptionalString(root, "dateTo");
                var reasoningValue = GetOptionalString(root, "reasoning");
                var statusMessageValue = GetOptionalString(root, "statusMessage");

                if (fromValue != null && !IsValidIataCode(fromValue))
                {
                    error = "Поле from должно быть IATA-кодом из 3 латинских букв или null.";
                    return false;
                }

                if (toValue != null && !IsValidIataCode(toValue))
                {
                    error = "Поле to должно быть IATA-кодом из 3 латинских букв или null.";
                    return false;
                }

                var minPriceValue = GetOptionalInt(root, "minPrice");
                var maxPriceValue = GetOptionalInt(root, "maxPrice");
                var maxDurationMinutesValue = GetOptionalInt(root, "maxDurationMinutes");
                if (!TryGetOptionalBool(root, "baggageRequired", out var baggageRequiredValue, out var baggageError))
                {
                    error = baggageError;
                    return false;
                }

                if (!TryGetOptionalBool(root, "suggestAlternative", out var suggestAlternativeValue, out var suggestAlternativeError))
                {
                    error = suggestAlternativeError;
                    return false;
                }

                if (!TryGetPassengers(root, out var passengersValue, out var passengersError))
                {
                    error = passengersError;
                    return false;
                }

                var allowedDateFormats = new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm" };

                if (dateValue != null && !DateTime.TryParseExact(dateValue, allowedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    error = "Поле date должно быть в формате YYYY-MM-DD или YYYY-MM-DDTHH:mm или null.";
                    return false;
                }

                if (dateFromValue != null && !DateTime.TryParseExact(dateFromValue, allowedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    error = "Поле dateFrom должно быть в формате YYYY-MM-DD или YYYY-MM-DDTHH:mm или null.";
                    return false;
                }

                if (dateToValue != null && !DateTime.TryParseExact(dateToValue, allowedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    error = "Поле dateTo должно быть в формате YYYY-MM-DD или YYYY-MM-DDTHH:mm или null.";
                    return false;
                }

                if (dateFromValue != null && dateToValue != null &&
                    DateTime.ParseExact(dateFromValue, allowedDateFormats, CultureInfo.InvariantCulture) >
                    DateTime.ParseExact(dateToValue, allowedDateFormats, CultureInfo.InvariantCulture))
                {
                    error = "Поле dateFrom не может быть позже dateTo.";
                    return false;
                }

                if (minPriceValue != null && minPriceValue < 0)
                {
                    error = "minPrice должен быть положительным числом или null.";
                    return false;
                }

                if (maxPriceValue != null && maxPriceValue < 0)
                {
                    error = "maxPrice должен быть положительным числом или null.";
                    return false;
                }

                if (minPriceValue != null && maxPriceValue != null && minPriceValue > maxPriceValue)
                {
                    error = "minPrice не может быть больше maxPrice.";
                    return false;
                }

                if (maxDurationMinutesValue != null && maxDurationMinutesValue < 1)
                {
                    error = "maxDurationMinutes должно быть положительным числом или null.";
                    return false;
                }

                if (fromValue == null && toValue == null && dateValue == null && dateFromValue == null && dateToValue == null)
                {
                    error = "Результат парсинга не содержит ни from, ни to, ни date, ни dateFrom/dateTo.";
                    return false;
                }

                response = new AiParseResponse(
                    From: fromValue,
                    To: toValue,
                    Date: dateValue,
                    DateFrom: dateFromValue,
                    DateTo: dateToValue,
                    Passengers: passengersValue,
                    MinPrice: minPriceValue,
                    MaxPrice: maxPriceValue,
                    MaxDurationMinutes: maxDurationMinutesValue,
                    BaggageRequired: baggageRequiredValue,
                    SuggestAlternative: suggestAlternativeValue,
                    StatusMessage: statusMessageValue,
                    Reasoning: reasoningValue
                );
                return true;
            }
            catch (JsonException ex)
            {
                error = $"JSON parse error: {ex.Message}";
                return false;
            }
        }

        private static bool ValidateAllowedProperties(JsonElement root, out string? error)
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "from",
                "to",
                "date",
                "dateFrom",
                "dateTo",
                "passengers",
                "minPrice",
                "maxPrice",
                "maxDurationMinutes",
                "baggageRequired",
                "suggestAlternative",
                "statusMessage",
                "reasoning"
            };

            foreach (var property in root.EnumerateObject())
            {
                if (!allowed.Contains(property.Name))
                {
                    error = $"Unexpected field: {property.Name}.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static string? GetOptionalString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return null;

            return element.ValueKind == JsonValueKind.String ? element.GetString()?.Trim() : null;
        }

        private static int? GetOptionalInt(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return null;

            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericValue))
                return numericValue;

            if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsedValue))
                return parsedValue;

            return null;
        }

        private static bool TryGetOptionalBool(JsonElement root, string propertyName, out bool? value, out string? error)
        {
            value = null;
            error = null;

            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return true;

            if (element.ValueKind == JsonValueKind.True)
            {
                value = true;
                return true;
            }

            if (element.ValueKind == JsonValueKind.False)
            {
                value = false;
                return true;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var text = element.GetString();
                if (bool.TryParse(text, out var parsedBool))
                {
                    value = parsedBool;
                    return true;
                }

                if (string.Equals(text, "да", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (string.Equals(text, "нет", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }
            }

            error = $"{propertyName} должен быть true, false или null.";
            return false;
        }

        private static bool TryGetPassengers(JsonElement root, out int passengers, out string? error)
        {
            passengers = 1;
            error = null;

            if (!root.TryGetProperty("passengers", out var passengersElement) || passengersElement.ValueKind == JsonValueKind.Null || passengersElement.ValueKind == JsonValueKind.Undefined)
                return true;

            if (passengersElement.ValueKind == JsonValueKind.Number && passengersElement.TryGetInt32(out var numericValue))
            {
                if (numericValue < 1 || numericValue > 5)
                {
                    error = "passengers должен быть числом от 1 до 5.";
                    return false;
                }

                passengers = numericValue;
                return true;
            }

            if (passengersElement.ValueKind == JsonValueKind.String && int.TryParse(passengersElement.GetString(), out var parsedValue))
            {
                if (parsedValue < 1 || parsedValue > 5)
                {
                    error = "passengers должен быть числом от 1 до 5.";
                    return false;
                }

                passengers = parsedValue;
                return true;
            }

            error = "passengers должен быть числом от 1 до 5 или null.";
            return false;
        }

        private static bool IsValidIataCode(string? code)
        {
            return !string.IsNullOrWhiteSpace(code)
                && code.Length == 3
                && code.All(ch => ch >= 'A' && ch <= 'Z');
        }

        internal static bool TryExtractJsonObject(string text, out string json)
        {
            json = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var cleanText = StripThink.Replace(text, string.Empty);
            cleanText = Regex.Replace(cleanText, "```(?:json)?\\s*|\\s*```", string.Empty, RegexOptions.IgnoreCase);
            cleanText = cleanText.Trim();
            var start = cleanText.IndexOf('{');
            if (start < 0)
                return false;

            var depth = 0;
            for (var i = start; i < cleanText.Length; i++)
            {
                if (cleanText[i] == '{') depth++;
                else if (cleanText[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        json = cleanText[start..(i + 1)];
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
