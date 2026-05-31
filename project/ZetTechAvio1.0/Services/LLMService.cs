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

    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    internal sealed record ChatMessage(string role, string content);
    internal sealed record ChatCompletionsRequest(string model, ChatMessage[] messages, double temperature = 0.1, int max_tokens = 400);
    internal sealed record ChatCompletionResponse(Choice[] choices);
    internal sealed record Choice(Message? message, string? text);
    internal sealed record Message(string role, string content, string? reasoning_content = null);

    public class LLMService : ILLMService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFlightsService _flightsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly int _retryDelaySeconds;
        private readonly int _maxTokens;
        private readonly bool _logRawLlmResponse;
        private static readonly Regex StripThink = new(@"<think>[\s\S]*?</think>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public LLMService(IHttpClientFactory httpClientFactory, IFlightsService flightsService, IConfiguration configuration, IDateTimeProvider? dateTimeProvider = null)
        {
            _httpClientFactory = httpClientFactory;
            _flightsService = flightsService;
            _dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();
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

            var airports = await _flightsService.GetAirportsAsync();
            string? lastError = null;
            for (var attempt = 1; attempt <= MaxLlmRetries; attempt++)
            {
                var outputText = await SendChatCompletionRequestAsync(client, endpoint, messages);
                AiParseResponse? parsedResponse = null;
                string? validationError = null;
                var parsedSuccessfully = TryParseAndValidateResponse(outputText, out parsedResponse, out validationError);

                if (!parsedSuccessfully)
                {
                    parsedSuccessfully = TryParseNaturalLanguageResponseWithAirports(outputText, airports, out parsedResponse, out validationError);
                }

                if (parsedSuccessfully)
                {
                    var (isValid, detailedError) = await ValidateParsedResponseAsync(parsedResponse!, text);
                    if (isValid)
                        return parsedResponse!;

                    lastError = detailedError ?? validationError ?? "LLM returned an invalid response.";
                }
                else
                {
                    lastError = validationError ?? "LLM returned an invalid response.";
                }

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
            var now = _dateTimeProvider.UtcNow;
            var airportList = await BuildAirportListAsync();
            var routeSummary = await BuildRouteSummaryAsync();
            var flightSchedule = await BuildFlightScheduleAsync();

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"Вы — агент парсинга запросов ZetTechAvio. Сегодня: {now:yyyy-MM-dd}, текущее время: {now:HH:mm} UTC.");
            promptBuilder.AppendLine("Отвечайте ТОЛЬКО одним JSON-объектом без markdown, без code fence, без пояснений и без текста вне JSON.");
            promptBuilder.AppendLine("Ответ должен быть в основном тексте ответа, а не только в поле reasoning_content или метаданных.");
            promptBuilder.AppendLine("Ответ должен начинаться с '{' и заканчиваться '}'.");
            promptBuilder.AppendLine("Если нужно подумать, используйте <think>, но итоговый ответ должен быть валидным JSON.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Список аэропортов, которые доступны в системе:");
            promptBuilder.AppendLine(airportList);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Актуальное расписание доступных рейсов и тарифов:");
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
            promptBuilder.AppendLine("8. Если запрос содержит диапазон цен, укажите minPrice и maxPrice.");
            promptBuilder.AppendLine("9. Если значение неизвестно, используйте null.");
            promptBuilder.AppendLine("9. Если город задан русским именем, сопоставьте его с IATA-кодом из списка выше.");
            promptBuilder.AppendLine("10. Если пользователь просит \"ближайшие рейсы\" или не указывает дату, оставьте date=null.");
            promptBuilder.AppendLine("11. Если запрос содержит «после X», установите dateFrom на X+1 день и dateTo=null.");
            promptBuilder.AppendLine("12. Если запрос содержит «до X», установите dateTo на X-1 день и dateFrom=null.");
            promptBuilder.AppendLine("13. Если запрос содержит «с X», установите dateFrom на X.");
            promptBuilder.AppendLine("14. Если запрос содержит «через N дней», вычислите date как сегодняшнюю дату + N.");
            promptBuilder.AppendLine("aaaa. Если дата запроса уже в прошлом относительно текущей UTC-даты, верните JSON с понятным statusMessage и reasoning, объясняющим ошибку. Backend отобразит это пользователю.");
            promptBuilder.AppendLine("aaaaa. Обязательно добавляйте statusMessage, если дата прошла. Не возвращайте обычный поисковый результат для прошедшей даты.");
            promptBuilder.AppendLine("16. Если запрос просит самый дешевый вариант, возвращайте параметры поиска для наиболее дешевого рейса, но не добавляйте посторонние альтернативы.");
            promptBuilder.AppendLine("17. Если запрос содержит «ближайший» или «следующий» рейс, учитывайте доступное расписание и ближайшие доступные даты.");
            promptBuilder.AppendLine("18. Относительные даты (сегодня, завтра, послезавтра, на следующей неделе, следующую пятницу, через 3 дня) разрешайте относительно текущей UTC-даты.");
            promptBuilder.AppendLine($"19. Сегодня UTC-дата: {now:yyyy-MM-dd}.");
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
            var now = _dateTimeProvider.UtcNow;
            var upcoming = flights
                .Where(f => f.DepartureDt >= now)
                .OrderBy(f => f.DepartureDt)
                .Take(100)
                .ToList();

            if (!upcoming.Any())
                return "Актуальных рейсов нет.";

            var descriptions = new List<string>(upcoming.Count);
            foreach (var f in upcoming)
            {
                var fares = await _flightsService.GetFlightFaresAsync(f.Id);
                var fareDescription = fares.Any()
                    ? string.Join("; ", fares.OrderBy(fa => fa.Price).Select(fa =>
                        $"{fa.Class} {fa.Price:F0} руб, места {fa.SeatsAvailable}, багаж {(fa.BaggageIncluded ? "включен" : "не включен")}, {(fa.Refundable ? "возвратный" : "невозвратный")}"))
                    : "тарифы не заданы";

                descriptions.Add($"{f.OriginAirport.City} ({f.OriginAirport.Iata}) -> {f.DestAirport.City} ({f.DestAirport.Iata}) | {f.DepartureDt:yyyy-MM-dd HH:mm} | {f.ArrivalDt:yyyy-MM-dd HH:mm} | рейс {f.FlightNumber} | тарифы: {fareDescription}");
            }

            return string.Join(Environment.NewLine, descriptions);
        }

        private static string BuildRetryPrompt(string error, string originalText)
        {
            return $"Предыдущий ответ был неверен: {error}. Верни строго один JSON-объект, начинающийся с '{{' и заканчивающегося '}}'. Не добавляй пояснений или текста вне JSON. Используй только поля from/to/date/dateFrom/dateTo/passengers/minPrice/maxPrice/maxDurationMinutes/baggageRequired/suggestAlternative/statusMessage/reasoning. Запрос: {originalText}";
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
            var contentBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(message?.content))
                contentBuilder.AppendLine(message.content.Trim());
            if (!string.IsNullOrWhiteSpace(message?.reasoning_content))
                contentBuilder.AppendLine(message.reasoning_content.Trim());
            if (!string.IsNullOrWhiteSpace(choice.text))
                contentBuilder.AppendLine(choice.text.Trim());

            var content = contentBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException($"LLM response content is empty. Response body: {responseBody}");

            return content;
        }

        internal static bool TryParseAndValidateResponse(string outputText, out AiParseResponse? response, out string? error)
        {
            response = null;
            error = null;

            if (string.IsNullOrWhiteSpace(outputText))
            {
                error = "LLM returned empty content.";
                return false;
            }

            var candidates = ExtractJsonCandidates(outputText).ToList();
            string? lastValidationError = null;
            foreach (var candidate in candidates)
            {
                if (TryParseAndValidateJson(candidate, out var parsed, out var validationError))
                {
                    response = parsed;
                    return true;
                }

                lastValidationError = validationError;
            }

            if (TryParseNaturalLanguageResponse(outputText, out response, out var fallbackError))
                return true;

            error = lastValidationError ?? fallbackError ?? "LLM output does not contain a valid JSON object.";
            return false;
        }

        private static bool TryParseNaturalLanguageResponse(string outputText, out AiParseResponse? response, out string? error)
        {
            response = null;
            error = null;

            if (string.IsNullOrWhiteSpace(outputText))
            {
                error = "LLM output does not contain parsable content.";
                return false;
            }

            var normalizedText = Regex.Replace(outputText, "\r?\n", " ", RegexOptions.Compiled).Trim();

            // 1. Ищем «финальную» стрелу, которой предшествует вывод-ключевик.
            var conclusionOrigin = ExtractConclusionArrow(normalizedText, out var conclusionDest);

            // 2. Ищем последнюю стрелу по всему тексту.
            var lastArrowOrigin = ExtractLastIataArrow(normalizedText, out var lastArrowDest);

            // 3. Ищем метки Origin/Destination. Убираем слишком широкие from/to.
            var phraseOrigin = ExtractIataFromPhrase(normalizedText, @"Origin|Отправление|Вылет из");
            var phraseDest = ExtractIataFromPhrase(normalizedText, @"Destination|Прибытие|В пункт назначения");

            var origin = conclusionOrigin ?? lastArrowOrigin ?? phraseOrigin;
            var destination = conclusionDest ?? lastArrowDest ?? phraseDest;

            if (origin == null && destination == null)
            {
                error = "No airports could be extracted from LLM reasoning.";
                return false;
            }

            var date = ExtractIsoDate(normalizedText);
            if (date == null)
            {
                var hasCheapest = Regex.IsMatch(normalizedText, @"\b(cheapest|дешев|самый дешев|лучший|lowest|минимум)\b", RegexOptions.IgnoreCase);
                var hasNearest = Regex.IsMatch(normalizedText, @"\b(closest|nearest|ближай|следующ)\b", RegexOptions.IgnoreCase);
                if (hasCheapest || hasNearest)
                {
                    date = null;
                }
            }

            TryExtractPriceRange(normalizedText, out var minPrice, out var maxPrice);

            response = new AiParseResponse(
                From: origin,
                To: destination,
                Date: date,
                DateFrom: null,
                DateTo: null,
                Passengers: 1,
                MinPrice: minPrice,
                MaxPrice: maxPrice,
                MaxDurationMinutes: null,
                BaggageRequired: null,
                SuggestAlternative: false,
                StatusMessage: null,
                Reasoning: "Парсинг из естественного языка LLM"
            );

            return true;
        }

        private static bool TryParseNaturalLanguageResponseWithAirports(string outputText, IReadOnlyCollection<Airport> airports, out AiParseResponse? response, out string? error)
        {
            response = null;
            error = null;

            if (TryParseNaturalLanguageResponse(outputText, out response, out error))
                return true;

            return TryParseAirportSearchFromText(outputText, airports, out response, out error);
        }

        private static bool TryParseAirportSearchFromText(string outputText, IReadOnlyCollection<Airport> airports, out AiParseResponse? response, out string? error)
        {
            response = null;
            error = null;

            if (string.IsNullOrWhiteSpace(outputText))
            {
                error = "LLM output does not contain parsable content.";
                return false;
            }

            var normalizedText = Regex.Replace(outputText, "\r?\n", " ", RegexOptions.Compiled).Trim();

            var origin = ExtractIataFromPhrase(normalizedText, @"Origin|From|Отправление|Вылет из");
            var destination = ExtractIataFromPhrase(normalizedText, @"Destination|To|Прибытие|В пункт назначения");

            origin ??= ExtractAirportCodeFromPlaceMention(normalizedText, @"Origin|From|Отправление|Вылет из", airports);
            destination ??= ExtractAirportCodeFromPlaceMention(normalizedText, @"Destination|To|Прибытие|В пункт назначения", airports);

            if ((origin == null || destination == null) && TryExtractCityPair(normalizedText, out var originCity, out var destinationCity))
            {
                origin ??= ResolveAirportCodeFromLocationName(originCity, airports);
                destination ??= ResolveAirportCodeFromLocationName(destinationCity, airports);
            }

            origin ??= ResolveAirportCodeFromLocationName(ExtractLocationNameAfterPreposition(normalizedText, @"\bиз\b"), airports);
            destination ??= ResolveAirportCodeFromLocationName(ExtractLocationNameAfterPreposition(normalizedText, @"\bв\b"), airports);

            if (origin == null && destination == null)
            {
                error = "Could not infer origin or destination from LLM reasoning.";
                return false;
            }

            var date = ExtractIsoDate(normalizedText);
            TryExtractPriceRange(normalizedText, out var minPrice, out var maxPrice);
            response = new AiParseResponse(
                From: origin,
                To: destination,
                Date: date,
                DateFrom: null,
                DateTo: null,
                Passengers: 1,
                MinPrice: minPrice,
                MaxPrice: maxPrice,
                MaxDurationMinutes: null,
                BaggageRequired: null,
                SuggestAlternative: false,
                StatusMessage: null,
                Reasoning: "Парсинг из естественного языка LLM"
            );

            return true;
        }

        private static string? ExtractAirportCodeFromPlaceMention(string text, string labelPattern, IReadOnlyCollection<Airport> airports)
        {
            var locationName = ExtractLocationNameFromPhrase(text, labelPattern);
            return ResolveAirportCodeFromLocationName(locationName, airports);
        }

        private static string? ExtractLocationNameFromPhrase(string text, string labelPattern)
        {
            var regex = new Regex($@"\b(?:{labelPattern})\b\s*[:\-–]?\s*([A-Za-zА-Яа-яЁё0-9]+(?:[ \.-][A-Za-zА-Яа-яЁё0-9]+)*?)(?=\s+(?:from|to|current|date|on|for|из|в|на|пункт|прибыти)\b|[\.,]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var match = regex.Match(text);
            if (!match.Success)
                return null;

            return match.Groups[1].Value.Trim();
        }

        private static string? ExtractLocationNameAfterPreposition(string text, string prepositionPattern)
        {
            var regex = new Regex($@"{prepositionPattern}\s+([A-Za-zА-Яа-яЁё]+(?:[ \-][A-Za-zА-Яа-яЁё]+)*?)(?=\s+(?:из|в|на|до|с|по|за|или|и|[.,;]|$))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var match = regex.Match(text);
            if (!match.Success)
                return null;

            return match.Groups[1].Value.Trim();
        }

        private static bool TryExtractPriceRange(string text, out int? minPrice, out int? maxPrice)
        {
            minPrice = null;
            maxPrice = null;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalizedText = text.ToLowerInvariant();

            var rangeMatch = Regex.Match(normalizedText, @"\bот\s+(\d{1,7})(?:\s*руб(?:\.|лей|ля)?)?\s+до\s+(\d{1,7})\b", RegexOptions.IgnoreCase);
            if (rangeMatch.Success)
            {
                minPrice = int.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                maxPrice = int.Parse(rangeMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                return true;
            }

            var maxMatch = Regex.Match(normalizedText, @"\bдо\s+(\d{1,7})(?:\s*руб(?:\.|лей|ля)?)?\b", RegexOptions.IgnoreCase);
            if (maxMatch.Success)
            {
                maxPrice = int.Parse(maxMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            var minMatch = Regex.Match(normalizedText, @"\bот\s+(\d{1,7})(?:\s*руб(?:\.|лей|ля)?)?\b", RegexOptions.IgnoreCase);
            if (minMatch.Success)
            {
                minPrice = int.Parse(minMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            return minPrice != null || maxPrice != null;
        }

        private static bool TryExtractCityPair(string text, out string origin, out string destination)
        {
            origin = null;
            destination = null;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalizedText = Regex.Replace(text, @"\bSt\.\s*", "St ", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            const string cityEn = @"[A-Za-z]+(?:[ \-][A-Za-z]+){0,3}";
            const string cityRu = @"[А-Яа-яЁё]+(?:[ \-][А-Яа-яЁё]+){0,3}";

            var patterns = new[]
            {
                new Regex($@"\bfrom\s+({cityEn})\s+to\s+({cityEn})(?=\s+(?:current|date|on|for|from|to|из|в|на|пункт|прибыти)\b|[.,;]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex($@"\bиз\s+({cityRu})\s+в\s+({cityRu})(?=\s+(?:current|date|on|for|from|to|из|в|на|пункт|прибыти)\b|[.,;]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex($@"\bиз\s+({cityRu})\s+на\s+({cityRu})(?=\s+(?:current|date|on|for|from|to|из|в|на|пункт|прибыти)\b|[.,;]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            };

            foreach (var regex in patterns)
            {
                var match = regex.Match(normalizedText);
                if (match.Success)
                {
                    origin = match.Groups[1].Value.Trim();
                    destination = match.Groups[2].Value.Trim();
                    return true;
                }
            }

            return false;
        }

        private static string? ResolveAirportCodeFromLocationName(string? location, IReadOnlyCollection<Airport> airports)
        {
            if (string.IsNullOrWhiteSpace(location))
                return null;

            var normalized = NormalizeLocationName(location);
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            if (AliasAirportCodes.TryGetValue(normalized, out var aliasCode))
                return aliasCode;

            var airport = airports.FirstOrDefault(a => string.Equals(NormalizeLocationName(a.City), normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeLocationName(a.Name), normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeLocationName(a.Iata), normalized, StringComparison.OrdinalIgnoreCase));

            if (airport != null)
                return airport.Iata;

            airport = airports.FirstOrDefault(a => NormalizeLocationName(a.City).Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || NormalizeLocationName(a.Name).Contains(normalized, StringComparison.OrdinalIgnoreCase));

            return airport?.Iata;
        }

        private static string NormalizeLocationName(string text)
        {
            return Regex.Replace(text.Trim().ToLowerInvariant(), "[\\s\\.\\-]+", " ");
        }

        private static readonly Dictionary<string, string> AliasAirportCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["moscow"] = "MOW",
            ["москва"] = "MOW",
            ["москвы"] = "MOW",
            ["москве"] = "MOW",
            ["москву"] = "MOW",
            ["москвой"] = "MOW",
            ["mow"] = "MOW",

            ["saint petersburg"] = "LED",
            ["st petersburg"] = "LED",
            ["st. petersburg"] = "LED",
            ["санкт петербург"] = "LED",
            ["санкт-петербург"] = "LED",
            ["санкт петербурга"] = "LED",
            ["санкт-петербурга"] = "LED",
            ["санкт петербурге"] = "LED",
            ["санкт-петербурге"] = "LED",
            ["санкт петербургу"] = "LED",
            ["санкт-петербургу"] = "LED",
            ["петербург"] = "LED",
            ["петербурга"] = "LED",
            ["петербурге"] = "LED",
            ["питер"] = "LED",
            ["spb"] = "LED",
            ["led"] = "LED",

            ["kazan"] = "KZN",
            ["казань"] = "KZN",
            ["казани"] = "KZN",
            ["казане"] = "KZN",
            ["казанью"] = "KZN",
            ["kzn"] = "KZN",

            ["sochi"] = "AER",
            ["сочи"] = "AER",
            ["aer"] = "AER",

            ["krasnodar"] = "KRR",
            ["краснодар"] = "KRR",
            ["краснодара"] = "KRR",
            ["краснодаре"] = "KRR",
            ["krr"] = "KRR",

            ["novosibirsk"] = "OVB",
            ["новосибирск"] = "OVB",
            ["новосибирска"] = "OVB",
            ["новосибирске"] = "OVB",
            ["ovb"] = "OVB",

            ["yekaterinburg"] = "SVX",
            ["екатеринбург"] = "SVX",
            ["екатеринбурга"] = "SVX",
            ["екатеринбурге"] = "SVX",
            ["svx"] = "SVX",

            ["minsk"] = "MSQ",
            ["минск"] = "MSQ",
            ["минска"] = "MSQ",
            ["минске"] = "MSQ",
            ["msq"] = "MSQ",

            ["tashkent"] = "TAS",
            ["ташкент"] = "TAS",
            ["ташкента"] = "TAS",
            ["ташкенте"] = "TAS",
            ["tas"] = "TAS",

            ["simferopol"] = "SIP",
            ["симферополь"] = "SIP",
            ["симферополя"] = "SIP",
            ["симферополе"] = "SIP",
            ["sip"] = "SIP",

            ["nizhny novgorod"] = "GOJ",
            ["нижний новгород"] = "GOJ",
            ["нижнего новгорода"] = "GOJ",
            ["нижнем новгороде"] = "GOJ",
            ["нижнему новгороду"] = "GOJ",
            ["нижним новгородом"] = "GOJ",
            ["goj"] = "GOJ"

            
        };

        private static string? ExtractIataFromPhrase(string text, string labelPattern)
        {
            var regex = new Regex($@"\b(?:{labelPattern})\b\s*[:\-–]?\s*[^\(\n]*?\(([A-Z]{{3}})\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var match = regex.Match(text);
            if (match.Success)
                return match.Groups[1].Value.ToUpperInvariant();

            var inlineRegex = new Regex($@"\b(?:{labelPattern})\b\s*[:\-–]?\s*([A-Z]{{3}})(?!\w)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            match = inlineRegex.Match(text);
            if (match.Success)
                return match.Groups[1].Value.ToUpperInvariant();

            return null;
        }

        private static string? ExtractConclusionArrow(string text, out string? destination)
        {
            destination = null;
            var matches = Regex.Matches(
                text,
                @"\b(?:cheapest|cheapest is|best|итого|лучший|самый дешевый|выбран|рекомендован)\b[^.]*?\b([A-Z]{3})\s*[-–>]+\s*([A-Z]{3})\b",
                RegexOptions.IgnoreCase);

            if (matches.Count == 0)
                return null;

            var lastMatch = matches[matches.Count - 1];
            destination = lastMatch.Groups[2].Value.ToUpperInvariant();
            return lastMatch.Groups[1].Value.ToUpperInvariant();
        }

        private static string? ExtractLastIataArrow(string text, out string? destination)
        {
            destination = null;
            var matches = Regex.Matches(text, @"\b([A-Z]{3})\s*[-–>]+\s*([A-Z]{3})\b");
            if (matches.Count == 0)
                return null;

            var last = matches[matches.Count - 1];
            destination = last.Groups[2].Value.ToUpperInvariant();
            return last.Groups[1].Value.ToUpperInvariant();
        }

        private static string? ExtractIataFromArrowPair(string text, out string? destination)
        {
            destination = null;
            var match = Regex.Match(text, @"\b([A-Z]{3})\s*[-–>]+\s*([A-Z]{3})\b");
            if (!match.Success)
                return null;

            destination = match.Groups[2].Value.ToUpperInvariant();
            return match.Groups[1].Value.ToUpperInvariant();
        }

        private static string? ExtractIsoDate(string text)
        {
            var match = Regex.Match(text, @"\b(\d{4}-\d{2}-\d{2})(?:T\d{2}:\d{2})?\b");
            if (match.Success)
                return match.Groups[1].Value;

            return null;
        }

        internal static IEnumerable<string> ExtractJsonCandidates(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var cleanText = StripThink.Replace(text, string.Empty);
            cleanText = Regex.Replace(cleanText, "```(?:json)?\\s*|\\s*```", string.Empty, RegexOptions.IgnoreCase);
            cleanText = cleanText.Trim();

            for (var start = 0; start < cleanText.Length; start++)
            {
                if (cleanText[start] != '{')
                    continue;

                var depth = 0;
                for (var i = start; i < cleanText.Length; i++)
                {
                    if (cleanText[i] == '{') depth++;
                    else if (cleanText[i] == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            var candidate = cleanText[start..(i + 1)];
                            if (IsValidJson(candidate))
                                yield return candidate;
                        }
                    }
                }
            }

            if (TryExtractJsonFromStructuredReasoning(cleanText, out var json) && IsValidJson(json))
                yield return json;
        }

        private static bool IsValidJson(string candidate)
        {
            try
            {
                using var document = JsonDocument.Parse(candidate);
                return document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
                return false;
            }
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

                var fromValue = NormalizeAirportCode(GetOptionalString(root, "from"));
                var toValue = NormalizeAirportCode(GetOptionalString(root, "to"));
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
                    Reasoning: reasoningValue ?? "Парсинг выполнен по ответу LLM."
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

        private static string? NormalizeAirportCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var trimmed = code.Trim();
            return trimmed.Length == 3 ? trimmed.ToUpperInvariant() : trimmed;
        }

        private async Task<(bool IsValid, string? Error)> ValidateParsedResponseAsync(AiParseResponse response, string requestText)
        {
            var airports = await _flightsService.GetAirportsAsync();
            var airportCodes = airports
                .Where(a => !string.IsNullOrWhiteSpace(a.Iata))
                .Select(a => a.Iata.Trim().ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (response.From != null && !airportCodes.Contains(response.From))
                return (false, "Поле from должно быть одним из доступных аэропортов.");

            if (response.To != null && !airportCodes.Contains(response.To))
                return (false, "Поле to должно быть одним из доступных аэропортов.");

            var today = _dateTimeProvider.UtcNow.Date;
            var allowedDateFormats = new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm" };

            if (response.Date != null &&
                DateTime.TryParseExact(response.Date, allowedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) &&
                parsedDate.Date < today &&
                !IsPastDateStatusMessage(response.StatusMessage))
            {
                return (false, "Поле date не может быть датой в прошлом без пояснения ошибки в statusMessage.");
            }

            if (response.DateFrom != null &&
                DateTime.TryParseExact(response.DateFrom, allowedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateFrom) &&
                parsedDateFrom.Date < today &&
                !IsPastDateStatusMessage(response.StatusMessage))
            {
                return (false, "Поле dateFrom не может быть датой в прошлом без пояснения ошибки в statusMessage.");
            }

            if (response.DateTo != null &&
                DateTime.TryParseExact(response.DateTo, allowedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTo) &&
                parsedDateTo.Date < today &&
                !IsPastDateStatusMessage(response.StatusMessage))
            {
                return (false, "Поле dateTo не может быть датой в прошлом без пояснения ошибки в statusMessage.");
            }

            if (RequiresNearestOrNextValidation(requestText) && response.From != null && response.To != null)
            {
                var flights = await _flightsService.GetAllFlightsAsync();
                var originCodes = GetAirportCodesForCityGroup(response.From, airports);
                var destCodes = GetAirportCodesForCityGroup(response.To, airports);
                var now = _dateTimeProvider.UtcNow;
                var upcomingFlights = flights
                    .Where(f => f.DepartureDt >= now)
                    .Where(f => originCodes.Contains(f.OriginAirport?.Iata, StringComparer.OrdinalIgnoreCase))
                    .Where(f => destCodes.Contains(f.DestAirport?.Iata, StringComparer.OrdinalIgnoreCase))
                    .OrderBy(f => f.DepartureDt)
                    .ToList();

                if (upcomingFlights.Any())
                {
                    if (response.Date != null &&
                        DateTime.TryParseExact(response.Date, allowedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate) &&
                        !upcomingFlights.Any(f => f.DepartureDt.Date == exactDate.Date))
                    {
                        return (false, "Дата запроса не соответствует ближайшим доступным рейсам для указанного маршрута.");
                    }

                    if ((response.DateFrom != null || response.DateTo != null) &&
                        DateTime.TryParseExact(response.DateFrom ?? response.DateTo!, allowedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        var rangeStart = response.DateFrom != null
                            ? DateTime.ParseExact(response.DateFrom, allowedDateFormats, CultureInfo.InvariantCulture).Date
                            : now.Date;
                        var rangeEnd = response.DateTo != null
                            ? DateTime.ParseExact(response.DateTo, allowedDateFormats, CultureInfo.InvariantCulture).Date
                            : DateTime.MaxValue.Date;

                        if (!upcomingFlights.Any(f => f.DepartureDt.Date >= rangeStart && f.DepartureDt.Date <= rangeEnd))
                        {
                            return (false, "Диапазон дат не содержит ближайших доступных рейсов для указанного маршрута.");
                        }
                    }
                }
            }

            return (true, null);
        }

        private static IReadOnlyCollection<string> GetAirportCodesForCityGroup(string iata, IEnumerable<Airport> airports)
        {
            var normalized = NormalizeAirportCode(iata);
            if (normalized == null)
                return Array.Empty<string>();

            var exactAirport = airports.FirstOrDefault(a => string.Equals(a.Iata, normalized, StringComparison.OrdinalIgnoreCase));
            if (exactAirport == null)
                return new[] { normalized };

            var sameCityCodes = airports
                .Where(a => !string.IsNullOrWhiteSpace(a.City) && string.Equals(a.City, exactAirport.City, StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Iata.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sameCityCodes.Count > 1 && string.Equals(exactAirport.Name, exactAirport.City, StringComparison.OrdinalIgnoreCase))
            {
                return sameCityCodes;
            }

            return new[] { normalized };
        }

        private static bool RequiresNearestOrNextValidation(string requestText)
        {
            if (string.IsNullOrWhiteSpace(requestText))
                return false;

            var normalized = requestText.ToLowerInvariant();
            return normalized.Contains("ближайш")
                || normalized.Contains("следующий рейс")
                || normalized.Contains("следующий перелет")
                || normalized.Contains("следующий вылет")
                || normalized.Contains("последующий рейс")
                || normalized.Contains("последующий перелет")
                || normalized.Contains("последующий вылет");
        }

        private static bool IsPastDateStatusMessage(string? statusMessage)
        {
            if (string.IsNullOrWhiteSpace(statusMessage))
                return false;

            var normalized = statusMessage.ToLowerInvariant();
            return normalized.Contains("прош") || normalized.Contains("истек") || normalized.Contains("прошла") || normalized.Contains("прошла дата") || normalized.Contains("уже") || normalized.Contains("ошибка");
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
            var candidate = ExtractJsonCandidates(text).FirstOrDefault();
            if (candidate == null)
                return false;

            json = candidate;
            return true;
        }

        private static bool TryExtractJsonFromStructuredReasoning(string text, out string json)
        {
            json = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var cleanText = Regex.Replace(text, "```(?:json)?\\s*|\\s*```", string.Empty, RegexOptions.IgnoreCase);
            cleanText = StripThink.Replace(cleanText, string.Empty).Trim();

            static string? ExtractFromAirportCode(string text)
            {
                var match = Regex.Match(text, @"(?:Origin|From):\s*.*?->\s*([A-Z]{3})", RegexOptions.IgnoreCase);
                return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
            }

            static string? ExtractToAirportCode(string text)
            {
                var match = Regex.Match(text, @"(?:Destination|To):\s*.*?->\s*([A-Z]{3})", RegexOptions.IgnoreCase);
                return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
            }

            var fromCode = ExtractFromAirportCode(cleanText);
            var toCode = ExtractToAirportCode(cleanText);
            if (string.IsNullOrWhiteSpace(fromCode) || string.IsNullOrWhiteSpace(toCode))
                return false;

            string? date = null;
            var dateMatch = Regex.Match(cleanText, @"Date:\s*([0-9]{4}-[0-9]{2}-[0-9]{2})", RegexOptions.IgnoreCase);
            if (dateMatch.Success)
            {
                date = dateMatch.Groups[1].Value;
            }
            else if (Regex.IsMatch(cleanText, @"Date:\s*(Not specified|null)", RegexOptions.IgnoreCase))
            {
                date = null;
            }

            var passengers = 1;
            var passengerMatch = Regex.Match(cleanText, @"Passengers:\s*(?:Default\s*)?(\d+)", RegexOptions.IgnoreCase);
            if (passengerMatch.Success && int.TryParse(passengerMatch.Groups[1].Value, out var parsedPassengers))
                passengers = parsedPassengers;

            var response = new AiParseResponse(
                From: fromCode,
                To: toCode,
                Date: date,
                Passengers: passengers,
                StatusMessage: "Parsed from model reasoning because a valid JSON object was not present.",
                Reasoning: "Fallback parsed from model reasoning content."
            );

            json = JsonSerializer.Serialize(response, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            return true;
        }
    }
}
