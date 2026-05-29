using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using ZetTechAvio1._0.Models;
using ZetTechAvio1._0.Services;

namespace ZetTechAvio1._0.Tests
{
    public class LLMServiceTests
    {
        [Fact]
        public void TryParseAndValidateJson_ReturnsParsedResponse_ForValidJson()
        {
            var json = "{\"from\":\"MOW\",\"to\":\"PAR\",\"date\":\"2026-05-30\",\"passengers\":2,\"reasoning\":\"поиск рейса Москва-Париж на 30 мая\"}";

            var result = LLMService.TryParseAndValidateJson(json, out var response, out var error);

            Assert.True(result, error);
            Assert.Null(error);
            Assert.NotNull(response);
            Assert.Equal("MOW", response!.From);
            Assert.Equal("PAR", response.To);
            Assert.Equal("2026-05-30", response.Date);
            Assert.Equal(2, response.Passengers);
            Assert.Equal("поиск рейса Москва-Париж на 30 мая", response.Reasoning);
        }

        [Fact]
        public void TryParseAndValidateJson_ReturnsParsedResponse_ForValidDateTimeJson()
        {
            var json = "{\"from\":\"MOW\",\"to\":\"PAR\",\"date\":\"2026-05-30T09:30\",\"passengers\":2,\"reasoning\":\"поиск рейса Москва-Париж на 30 мая 09:30\"}";

            var result = LLMService.TryParseAndValidateJson(json, out var response, out var error);

            Assert.True(result, error);
            Assert.Null(error);
            Assert.NotNull(response);
            Assert.Equal("MOW", response!.From);
            Assert.Equal("PAR", response.To);
            Assert.Equal("2026-05-30T09:30", response.Date);
            Assert.Equal(2, response.Passengers);
            Assert.Equal("поиск рейса Москва-Париж на 30 мая 09:30", response.Reasoning);
        }

        [Fact]
        public void TryParseAndValidateJson_ReturnsFalse_ForInvalidDateTimeFormat()
        {
            var json = "{\"from\":\"MOW\",\"to\":\"PAR\",\"date\":\"30-05-2026 09:30\",\"passengers\":2,\"reasoning\":\"тест\"}";

            var result = LLMService.TryParseAndValidateJson(json, out var response, out var error);

            Assert.False(result);
            Assert.Null(response);
            Assert.Equal("Поле date должно быть в формате YYYY-MM-DD или YYYY-MM-DDTHH:mm или null.", error);
        }

        [Fact]
        public void TryParseAndValidateJson_ReturnsFalse_ForUnexpectedField()
        {
            var json = "{\"from\":\"MOW\",\"to\":\"PAR\",\"date\":\"2026-05-30\",\"passengers\":2,\"reasoning\":\"тест\",\"extra\":\"no\"}";

            var result = LLMService.TryParseAndValidateJson(json, out var response, out var error);

            Assert.False(result);
            Assert.Null(response);
            Assert.Equal("Unexpected field: extra.", error);
        }

        [Fact]
        public void TryParseAndValidateJson_ReturnsFalse_ForInvalidDateFormat()
        {
            var json = "{\"from\":\"MOW\",\"to\":\"PAR\",\"date\":\"30.05.2026\",\"passengers\":2,\"reasoning\":\"тест\"}";

            var result = LLMService.TryParseAndValidateJson(json, out var response, out var error);

            Assert.False(result);
            Assert.Null(response);
            Assert.Equal("Поле date должно быть в формате YYYY-MM-DD или YYYY-MM-DDTHH:mm или null.", error);
        }

        [Fact]
        public void TryParseAndValidateJson_ReturnsFalse_ForInvalidIataCode()
        {
            var json = "{\"from\":\"Бар\",\"to\":\"PAR\",\"date\":\"2026-05-30\",\"passengers\":2,\"reasoning\":\"тест\"}";

            var result = LLMService.TryParseAndValidateJson(json, out var response, out var error);

            Assert.False(result);
            Assert.Null(response);
            Assert.Equal("Поле from должно быть IATA-кодом из 3 латинских букв или null.", error);
        }

        [Fact]
        public void TryExtractJsonObject_ReturnsJsonFromFencedCodeBlock()
        {
            var text = "Вот ответ:\n```json\n{\"from\":\"MOW\",\"to\":\"LED\",\"date\":null,\"passengers\":1,\"reasoning\":\"самый дешевый рейс\"}\n```";

            var result = LLMService.TryExtractJsonObject(text, out var json);

            Assert.True(result);
            Assert.Equal("{\"from\":\"MOW\",\"to\":\"LED\",\"date\":null,\"passengers\":1,\"reasoning\":\"самый дешевый рейс\"}", json);
        }

        [Fact]
        public void TryParseAndValidateJson_ReturnsExtendedFields_ForValidRangeJson()
        {
            var json = "{\"from\":\"MOW\",\"to\":\"PAR\",\"dateFrom\":\"2026-06-10\",\"dateTo\":\"2026-06-20\",\"passengers\":2,\"minPrice\":3000,\"maxPrice\":12000,\"maxDurationMinutes\":320,\"baggageRequired\":true,\"suggestAlternative\":false,\"statusMessage\":\"предложения с багажом\",\"reasoning\":\"выбран диапазон дат\"}";

            var result = LLMService.TryParseAndValidateJson(json, out var response, out var error);

            Assert.True(result, error);
            Assert.Null(error);
            Assert.NotNull(response);
            Assert.Equal("MOW", response!.From);
            Assert.Equal("PAR", response.To);
            Assert.Null(response.Date);
            Assert.Equal("2026-06-10", response.DateFrom);
            Assert.Equal("2026-06-20", response.DateTo);
            Assert.Equal(2, response.Passengers);
            Assert.Equal(3000, response.MinPrice);
            Assert.Equal(12000, response.MaxPrice);
            Assert.Equal(320, response.MaxDurationMinutes);
            Assert.True(response.BaggageRequired);
            Assert.False(response.SuggestAlternative);
            Assert.Equal("предложения с багажом", response.StatusMessage);
            Assert.Equal("выбран диапазон дат", response.Reasoning);
        }

        [Fact]
        public async Task ParseFlightSearchAsync_Parses25RealisticQueries()
        {
            var testCases = GetSampleLlmQueries();
            var handler = new FakeHttpMessageHandler(async request =>
            {
                var body = request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync();
                string? userContent = null;
                try
                {
                    using var document = JsonDocument.Parse(body);
                    var root = document.RootElement;
                    if (root.TryGetProperty("messages", out var messages))
                    {
                        foreach (var message in messages.EnumerateArray())
                        {
                            if (message.TryGetProperty("role", out var role) && role.GetString() is "user" &&
                                message.TryGetProperty("content", out var contentElement))
                            {
                                userContent = contentElement.GetString() ?? string.Empty;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    userContent = body;
                }

                var testCase = testCases.FirstOrDefault(tc =>
                    userContent?.Contains(tc.RequestText, StringComparison.OrdinalIgnoreCase) == true);

                if (testCase == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("{\"error\":\"unknown query\"}", Encoding.UTF8, "application/json")
                    };
                }

                var content = JsonSerializer.Serialize(new
                {
                    choices = new[]
                    {
                        new
                        {
                            message = new { role = "assistant", content = testCase.ResponseJson }
                        }
                    }
                });

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test-openai.local/")
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { { "LLM:ApiKey", "test-key" } })
                .Build();

            var llmService = new LLMService(new StubHttpClientFactory(httpClient), new TestFlightsService(), configuration);

            foreach (var testCase in testCases)
            {
                var result = await llmService.ParseFlightSearchAsync(testCase.RequestText);
                Assert.NotNull(result);
                AssertEqualWithContext(testCase.Expected.From, result.From, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.To, result.To, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.Date, result.Date, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.DateFrom, result.DateFrom, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.DateTo, result.DateTo, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.Passengers, result.Passengers, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.MinPrice, result.MinPrice, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.MaxPrice, result.MaxPrice, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.MaxDurationMinutes, result.MaxDurationMinutes, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.BaggageRequired, result.BaggageRequired, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.SuggestAlternative, result.SuggestAlternative, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.StatusMessage, result.StatusMessage, testCase.RequestText);
                AssertEqualWithContext(testCase.Expected.Reasoning, result.Reasoning, testCase.RequestText);
            }
        }

        [Fact]
        public async Task ParseFlightSearchAsync_RealLlmRequestsSequentially_WhenEnvEnabled()
        {
            var enableRealTest = Environment.GetEnvironmentVariable("ZETTECHAVIO_REAL_LLM_TEST")?.Equals("1", StringComparison.OrdinalIgnoreCase) == true;
            if (!enableRealTest)
            {
                return;
            }

            var apiKey = Environment.GetEnvironmentVariable("LLM__ApiKey") ?? Environment.GetEnvironmentVariable("LLM:ApiKey");
            Assert.False(string.IsNullOrWhiteSpace(apiKey), "Set LLM__ApiKey or LLM:ApiKey in environment to run real LLM tests.");

            var baseUrl = Environment.GetEnvironmentVariable("LLM__BaseUrl") ?? Environment.GetEnvironmentVariable("LLM:BaseUrl") ?? "https://gives-controllers-fine-remedies.trycloudflare.com";
            var modelName = Environment.GetEnvironmentVariable("LLM__Model") ?? Environment.GetEnvironmentVariable("LLM:Model") ?? "qwen/qwen3.5-9b";
            var delaySeconds = int.TryParse(Environment.GetEnvironmentVariable("ZETTECHAVIO_REAL_LLM_REQUEST_DELAY_SECONDS"), out var parsedDelay)
                ? Math.Max(parsedDelay, 1)
                : 15;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "LLM:ApiKey", apiKey },
                    { "LLM:BaseUrl", baseUrl },
                    { "LLM:Model", modelName },
                    { "LLM:LogRawResponse", "true" }
                })
                .Build();

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(120)
            };

            var llmService = new LLMService(new StubHttpClientFactory(httpClient), new TestFlightsService(), configuration);
            var queries = new[]
            {
                "Хочу полететь по самому дешевому рейсу из Москвы в Питер",
                "Найди ближайший рейс из Санкт-Петербурга в Москву",
                "Рейс Москва - Новосибирск после 15 июля"
            };

            for (var i = 0; i < queries.Length; i++)
            {
                if (i > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }

                var result = await llmService.ParseFlightSearchAsync(queries[i]);
                Assert.NotNull(result);
                Assert.NotNull(result.Reasoning);
            }
        }

        [Fact]
        public async Task ParseFlightSearchAsync_RealLlmRequestsSequentially_TenQueries_WhenEnvEnabled()
        {
            var enableRealTest = Environment.GetEnvironmentVariable("ZETTECHAVIO_REAL_LLM_TEST")?.Equals("1", StringComparison.OrdinalIgnoreCase) == true;
            if (!enableRealTest)
            {
                return;
            }

            var apiKey = Environment.GetEnvironmentVariable("LLM__ApiKey") ?? Environment.GetEnvironmentVariable("LLM:ApiKey");
            Assert.False(string.IsNullOrWhiteSpace(apiKey), "Set LLM__ApiKey or LLM:ApiKey in environment to run real LLM tests.");

            var baseUrl = Environment.GetEnvironmentVariable("LLM__BaseUrl") ?? Environment.GetEnvironmentVariable("LLM:BaseUrl") ?? "https://gives-controllers-fine-remedies.trycloudflare.com";
            var modelName = Environment.GetEnvironmentVariable("LLM__Model") ?? Environment.GetEnvironmentVariable("LLM:Model") ?? "qwen/qwen3.5-9b";
            var delaySeconds = int.TryParse(Environment.GetEnvironmentVariable("ZETTECHAVIO_REAL_LLM_REQUEST_DELAY_SECONDS"), out var parsedDelay)
                ? Math.Max(parsedDelay, 1)
                : 15;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "LLM:ApiKey", apiKey },
                    { "LLM:BaseUrl", baseUrl },
                    { "LLM:Model", modelName },
                    { "LLM:LogRawResponse", "true" }
                })
                .Build();

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(120)
            };

            var llmService = new LLMService(new StubHttpClientFactory(httpClient), new TestFlightsService(), configuration);
            var queries = new[]
            {
                "Хочу полететь по самому дешевому рейсу из Москвы в Питер",
                "Найди ближайший рейс из Санкт-Петербурга в Москву",
                "Рейс Москва - Новосибирск после 15 июля",
                "Ищу рейсы МСК -> ПИТЕР на послезавтра",
                "Полет из Домодедово в Пулково 30 июня на двоих",
                "Самый дешевый вариант в Сочи, если не получится, то Краснодар",
                "Лучший рейс из Москвы в Екатеринбург",
                "Полет домой из Москвы в Ташкент 3 июля",
                "Нужен рейс Москва -> Минск на 20 мая",
                "Ищу авиабилет из Москвы в Санкт-Петербург, не дороже 5000"
            };

            for (var i = 0; i < queries.Length; i++)
            {
                if (i > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }

                var requestText = queries[i];
                Console.WriteLine($"\n--- LLM Request #{i + 1} ---");
                Console.WriteLine(requestText);

                var result = await llmService.ParseFlightSearchAsync(requestText);
                Assert.NotNull(result);
                Assert.NotNull(result.Reasoning);

                Console.WriteLine("Parsed response:");
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        private static void AssertEqualWithContext<T>(T? expected, T? actual, string requestText)
        {
            var expectedText = expected is null ? "null" : expected.ToString();
            var actualText = actual is null ? "null" : actual.ToString();
            Assert.True(EqualityComparer<T?>.Default.Equals(expected, actual),
                $"Request: {requestText}. Expected: {expectedText}, Actual: {actualText}.");
        }

        private static List<LlmQueryTestCase> GetSampleLlmQueries()
        {
            return new List<LlmQueryTestCase>
            {
                new("Хочу полететь по самому дешевому рейсу из Москвы в Питер",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 1, reasoning = "самый дешевый рейс из Москвы в Питер" }),
                    ExpectedResponse(from: "MOW", to: "LED", reasoning: "самый дешевый рейс из Москвы в Питер")),
                new("Найди ближайший рейс из Санкт-Петербурга в Москву",
                    JsonSerializer.Serialize(new { from = "LED", to = "MOW", date = (string?)null, passengers = 1, reasoning = "ближайший рейс из Санкт-Петербурга в Москву" }),
                    ExpectedResponse(from: "LED", to: "MOW", reasoning: "ближайший рейс из Санкт-Петербурга в Москву")),
                new("Ищу рейсы МСК -> ПИТЕР на послезавтра",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = "2026-05-31", passengers = 1, reasoning = "послезавтра из Москвы в Питер" }),
                    ExpectedResponse(from: "MOW", to: "LED", date: "2026-05-31", reasoning: "послезавтра из Москвы в Питер")),
                new("Полет из Домодедово в Пулково 30 июня на двоих",
                    JsonSerializer.Serialize(new { from = "DME", to = "LED", date = "2026-06-30", passengers = 2, reasoning = "рейс из Домодедово в Пулково 30 июня на двоих" }),
                    ExpectedResponse(from: "DME", to: "LED", date: "2026-06-30", passengers: 2, reasoning: "рейс из Домодедово в Пулково 30 июня на двоих")),
                new("Самый дешевый вариант в Сочи, если не получится, то Краснодар",
                    JsonSerializer.Serialize(new { from = "MOW", to = "AER", date = (string?)null, passengers = 1, suggestAlternative = true, statusMessage = "если Сочи недоступен, предложить Краснодар", reasoning = "самый дешевый рейс в Сочи с альтернативой Краснодар" }),
                    ExpectedResponse(from: "MOW", to: "AER", suggestAlternative: true, statusMessage: "если Сочи недоступен, предложить Краснодар", reasoning: "самый дешевый рейс в Сочи с альтернативой Краснодар")),
                new("Рейс Москва - Новосибирск после 15 июля",
                    JsonSerializer.Serialize(new { from = "MOW", to = "OVB", dateFrom = "2026-07-16", dateTo = (string?)null, passengers = 1, reasoning = "рейс Москва - Новосибирск после 15 июля" }),
                    ExpectedResponse(from: "MOW", to: "OVB", dateFrom: "2026-07-16", reasoning: "рейс Москва - Новосибирск после 15 июля")),
                new("Лучший рейс из Москвы в Екатеринбург",
                    JsonSerializer.Serialize(new { from = "MOW", to = "SVX", date = (string?)null, passengers = 1, reasoning = "лучший рейс из Москвы в Екатеринбург" }),
                    ExpectedResponse(from: "MOW", to: "SVX", reasoning: "лучший рейс из Москвы в Екатеринбург")),
                new("Рейс из Москвы в Санкт-Петербург с багажом",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 1, baggageRequired = true, reasoning = "рейс из Москвы в Санкт-Петербург с багажом" }),
                    ExpectedResponse(from: "MOW", to: "LED", baggageRequired: true, reasoning: "рейс из Москвы в Санкт-Петербург с багажом")),
                new("Полет на следующей неделе из Питера в Москву",
                    JsonSerializer.Serialize(new { from = "LED", to = "MOW", dateFrom = "2026-06-05", dateTo = "2026-06-11", passengers = 1, reasoning = "рейсы на следующей неделе из Питера в Москву" }),
                    ExpectedResponse(from: "LED", to: "MOW", dateFrom: "2026-06-05", dateTo: "2026-06-11", reasoning: "рейсы на следующей неделе из Питера в Москву")),
                new("Ищу рейс из Внуково в Казань на 1 пассажира",
                    JsonSerializer.Serialize(new { from = "VKO", to = "KZN", date = (string?)null, passengers = 1, reasoning = "рейс из Внуково в Казань на одного пассажира" }),
                    ExpectedResponse(from: "VKO", to: "KZN", reasoning: "рейс из Внуково в Казань на одного пассажира")),
                new("Нужен рейс Москва -> Минск на 20 мая",
                    JsonSerializer.Serialize(new { from = "MOW", to = "MSQ", date = "2026-05-20", passengers = 1, reasoning = "рейс Москва - Минск на 20 мая" }),
                    ExpectedResponse(from: "MOW", to: "MSQ", date: "2026-05-20", reasoning: "рейс Москва - Минск на 20 мая")),
                new("Рейс из Санкт-Петербург в Симферополь на 2 взрослых",
                    JsonSerializer.Serialize(new { from = "LED", to = "SIP", date = (string?)null, passengers = 2, reasoning = "рейс из Санкт-Петербурга в Симферополь на двух взрослых" }),
                    ExpectedResponse(from: "LED", to: "SIP", passengers: 2, reasoning: "рейс из Санкт-Петербурга в Симферополь на двух взрослых")),
                new("Полет домой из Москвы в Ташкент 3 июля",
                    JsonSerializer.Serialize(new { from = "MOW", to = "TAS", date = "2026-07-03", passengers = 1, reasoning = "полет домой из Москвы в Ташкент 3 июля" }),
                    ExpectedResponse(from: "MOW", to: "TAS", date: "2026-07-03", reasoning: "полет домой из Москвы в Ташкент 3 июля")),
                new("Ближайший рейс до Питера",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 1, reasoning = "ближайший рейс до Питера" }),
                    ExpectedResponse(from: "MOW", to: "LED", reasoning: "ближайший рейс до Питера")),
                new("Самый дешевый перелет из Москвы в Санкт-Петербург на выходные",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", dateFrom = "2026-05-31", dateTo = "2026-06-01", passengers = 1, reasoning = "самый дешевый перелет на выходные" }),
                    ExpectedResponse(from: "MOW", to: "LED", dateFrom: "2026-05-31", dateTo: "2026-06-01", reasoning: "самый дешевый перелет на выходные")),
                new("Ищу авиабилет из Москвы в Санкт-Петербург, не дороже 5000",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 1, maxPrice = 5000, reasoning = "рейс из Москвы в Санкт-Петербург до 5000" }),
                    ExpectedResponse(from: "MOW", to: "LED", maxPrice: 5000, reasoning: "рейс из Москвы в Санкт-Петербург до 5000")),
                new("Рейс из Москвы в Питер без багажа",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 1, baggageRequired = false, reasoning = "рейс из Москвы в Питер без багажа" }),
                    ExpectedResponse(from: "MOW", to: "LED", baggageRequired: false, reasoning: "рейс из Москвы в Питер без багажа")),
                new("Если нет туда, то обратно Москва-СПб",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 1, suggestAlternative = true, statusMessage = "если нет туда, предложить обратно", reasoning = "если нет туда, то обратно Москва-СПб" }),
                    ExpectedResponse(from: "MOW", to: "LED", suggestAlternative: true, statusMessage: "если нет туда, предложить обратно", reasoning: "если нет туда, то обратно Москва-СПб")),
                new("Полет на 4 человек из МСК в СПб",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 4, reasoning = "рейс на 4 человек из Москвы в Санкт-Петербург" }),
                    ExpectedResponse(from: "MOW", to: "LED", passengers: 4, reasoning: "рейс на 4 человек из Москвы в Санкт-Петербург")),
                new("Нужен рейс Москва->Питер 2026-06-01",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = "2026-06-01", passengers = 1, reasoning = "рейс Москва - Питер 1 июня 2026" }),
                    ExpectedResponse(from: "MOW", to: "LED", date: "2026-06-01", reasoning: "рейс Москва - Питер 1 июня 2026")),
                new("Самый дешевый из Москвы в Петербург на следующую среду",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = "2026-06-04", passengers = 1, reasoning = "рейс на следующую среду" }),
                    ExpectedResponse(from: "MOW", to: "LED", date: "2026-06-04", reasoning: "рейс на следующую среду")),
                new("Найди рейс из Домодедово в Пулково на сегодня",
                    JsonSerializer.Serialize(new { from = "DME", to = "LED", date = "2026-05-29", passengers = 1, reasoning = "рейс на сегодня" }),
                    ExpectedResponse(from: "DME", to: "LED", date: "2026-05-29", reasoning: "рейс на сегодня")),
                new("Рейс Москва Санкт-Петербург на 2026-06-15 10:00",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = "2026-06-15T10:00", passengers = 1, reasoning = "рейс Москва - Санкт-Петербург 15 июня 10:00" }),
                    ExpectedResponse(from: "MOW", to: "LED", date: "2026-06-15T10:00", reasoning: "рейс Москва - Санкт-Петербург 15 июня 10:00")),
                new("Запрос: прилет в Питер, вылет из Москвы, 3 пассажира",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 3, reasoning = "прилет в Питер, вылет из Москвы, 3 пассажира" }),
                    ExpectedResponse(from: "MOW", to: "LED", passengers: 3, reasoning: "прилет в Питер, вылет из Москвы, 3 пассажира")),
                new("Хочу рейс Москва - Санкт-Петербург, если не получится, то Нижний Новгород",
                    JsonSerializer.Serialize(new { from = "MOW", to = "LED", date = (string?)null, passengers = 1, suggestAlternative = true, statusMessage = "если Санкт-Петербург недоступен, предложить Нижний Новгород", reasoning = "рейс Москва - Санкт-Петербург с альтернативой Нижний Новгород" }),
                    ExpectedResponse(from: "MOW", to: "LED", suggestAlternative: true, statusMessage: "если Санкт-Петербург недоступен, предложить Нижний Новгород", reasoning: "рейс Москва - Санкт-Петербург с альтернативой Нижний Новгород"))
            };
        }

        private static AiParseResponse ExpectedResponse(
            string? from,
            string? to,
            string? date = null,
            string? dateFrom = null,
            string? dateTo = null,
            int? passengers = 1,
            int? minPrice = null,
            int? maxPrice = null,
            int? maxDurationMinutes = null,
            bool? baggageRequired = null,
            bool? suggestAlternative = null,
            string? statusMessage = null,
            string? reasoning = null)
            => new AiParseResponse(from, to, date, dateFrom, dateTo, passengers, minPrice, maxPrice, maxDurationMinutes, baggageRequired, suggestAlternative, statusMessage, reasoning);

        private sealed record LlmQueryTestCase(string RequestText, string ResponseJson, AiParseResponse Expected);
    }

    internal class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    internal class TestFlightsService : IFlightsService
    {
        private static readonly List<Airport> Airports = new()
        {
            new Airport { Id = 1, Iata = "MOW", Name = "Москва", City = "Москва", Country = "Россия" },
            new Airport { Id = 2, Iata = "LED", Name = "Пулково", City = "Санкт-Петербург", Country = "Россия" },
            new Airport { Id = 3, Iata = "DME", Name = "Домодедово", City = "Москва", Country = "Россия" },
            new Airport { Id = 4, Iata = "VKO", Name = "Внуково", City = "Москва", Country = "Россия" },
            new Airport { Id = 5, Iata = "KZN", Name = "Казань", City = "Казань", Country = "Россия" },
            new Airport { Id = 6, Iata = "AER", Name = "Сочи", City = "Сочи", Country = "Россия" },
            new Airport { Id = 7, Iata = "KRR", Name = "Краснодар", City = "Краснодар", Country = "Россия" },
            new Airport { Id = 8, Iata = "OVB", Name = "Толмачёво", City = "Новосибирск", Country = "Россия" },
            new Airport { Id = 9, Iata = "SVX", Name = "Кольцово", City = "Екатеринбург", Country = "Россия" },
            new Airport { Id = 10, Iata = "MSQ", Name = "Минск", City = "Минск", Country = "Беларусь" },
            new Airport { Id = 11, Iata = "SIP", Name = "Симферополь", City = "Симферополь", Country = "Россия" },
            new Airport { Id = 12, Iata = "TAS", Name = "Ташкент", City = "Ташкент", Country = "Узбекистан" },
            new Airport { Id = 13, Iata = "GOJ", Name = "Стригино", City = "Нижний Новгород", Country = "Россия" }
        };

        private static readonly List<FlightDto> Flights = new()
        {
            new FlightDto { Id = 1, FlightNumber = "SU101", DurationMinutes = 90, DepartureDt = DateTime.UtcNow.AddHours(5), ArrivalDt = DateTime.UtcNow.AddHours(7), MinPrice = 3500, BaggageInfo = "Багаж включен", OriginAirport = Airports[0], DestAirport = Airports[1], AirlineId = 1, AircraftId = 1, OriginAirportId = 1, DestAirportId = 2, Status = "OnTime" },
            new FlightDto { Id = 2, FlightNumber = "SU102", DurationMinutes = 95, DepartureDt = DateTime.UtcNow.AddDays(1), ArrivalDt = DateTime.UtcNow.AddDays(1).AddHours(2), MinPrice = 3200, BaggageInfo = "Багаж включен", OriginAirport = Airports[2], DestAirport = Airports[1], AirlineId = 1, AircraftId = 1, OriginAirportId = 3, DestAirportId = 2, Status = "OnTime" },
            new FlightDto { Id = 3, FlightNumber = "SU103", DurationMinutes = 200, DepartureDt = DateTime.UtcNow.AddDays(7), ArrivalDt = DateTime.UtcNow.AddDays(7).AddHours(3), MinPrice = 15000, BaggageInfo = "Багаж включен", OriginAirport = Airports[0], DestAirport = Airports[5], AirlineId = 1, AircraftId = 1, OriginAirportId = 1, DestAirportId = 6, Status = "OnTime" },
            new FlightDto { Id = 4, FlightNumber = "SU104", DurationMinutes = 270, DepartureDt = DateTime.UtcNow.AddMonths(1), ArrivalDt = DateTime.UtcNow.AddMonths(1).AddHours(4), MinPrice = 30000, BaggageInfo = "Багаж включен", OriginAirport = Airports[0], DestAirport = Airports[7], AirlineId = 1, AircraftId = 1, OriginAirportId = 1, DestAirportId = 8, Status = "OnTime" }
        };

        public Task<List<FlightDto>> GetAllFlightsAsync() => Task.FromResult(Flights);
        public Task<List<FlightDto>> SearchFlightsAsync(string from, string to, string date) => Task.FromResult(new List<FlightDto>());
        public Task<Flight?> GetFlightByIdAsync(int id) => Task.FromResult<Flight?>(null);
        public Task<Flight> CreateFlightAsync(Flight flight) => throw new NotImplementedException();
        public Task<Flight?> UpdateFlightAsync(int id, Flight updatedFlight) => throw new NotImplementedException();
        public Task<DeleteFlightResult> DeleteFlightAsync(int id) => throw new NotImplementedException();
        public Task<List<Fare>> GetFlightFaresAsync(int flightId) => throw new NotImplementedException();
        public Task<List<Airport>> GetAirportsAsync() => Task.FromResult(Airports);
        public Task<Airport?> GetAirportByIdAsync(int airportId) => Task.FromResult(Airports.FirstOrDefault(a => a.Id == airportId));
        public Task<List<Airline>> GetAirlinesAsync() => Task.FromResult(new List<Airline>());
        public Task<Airline?> GetAirlineByIdAsync(int airlineId) => Task.FromResult<Airline?>(null);
        public Task<string?> ValidateAirportRouteAsync(int originAirportId, int destAirportId) => Task.FromResult<string?>(null);
        public Task<string> GenerateFlightNumberAsync(string airlinePrefix) => throw new NotImplementedException();
        public Task<List<Aircraft>> GetAircraftsAsync() => Task.FromResult(new List<Aircraft>());
        public Task<int> GetFlightTicketCountAsync(int flightId) => throw new NotImplementedException();
        public Task<List<Flight>> CreateScheduledFlightsAsync(FlightScheduleRequest request) => Task.FromResult(new List<Flight>());
    }

    internal class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return _responder(request);
        }
    }
}
