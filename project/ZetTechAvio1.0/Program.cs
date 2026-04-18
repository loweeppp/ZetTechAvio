using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ZetTechAvio1._0.Components;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=SERVER_NAME;Database=DB_NAME;User=DB_USER;Password=DB_PASSWORD;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, 
        ServerVersion.AutoDetect(connectionString)));

// Регистрация сервисов
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();
builder.Services.AddScoped<IUserValidationService, UserValidationService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthStateService, AuthStateService>();
builder.Services.AddScoped<IFaresService, FaresService>();
builder.Services.AddScoped<IFlightsService, FlightsService>();
builder.Services.AddScoped<IBookingsService, BookingsService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IConfirmationService>(sp => 
    new ConfirmationService(
    sp.GetRequiredService<ApplicationDbContext>(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<IWebHostEnvironment>(),
    sp.GetRequiredService<ILogger<ConfirmationService>>()));
        //  builder.Configuration));


// Конфигурация HttpClient для внешних запросов (YooKassa, etc.)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<HttpClient>(sp =>
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
        return new HttpClient(handler);
    });
}
else
{
    // Production: используем стандартный HttpClient с поддержкой DNS
    builder.Services.AddScoped<HttpClient>(sp =>
    {
        var handler = new HttpClientHandler();
        handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
        // Используем системные DNS resolver
        handler.UseCookies = true;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        return client;
    });
}

// Также зарегистрируем стандартный HttpClientFactory
builder.Services.AddHttpClient();

// Поддержка Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Настройка CORS для разрешения запросов с React-приложения
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(";") ?? new[] { "http://localhost:3000" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    // Отдельная политика для webhook (без ограничений по Origin)
    options.AddPolicy("AllowWebhook", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Добавляем JWT аутентификацию
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "fallback-secret-key-for-development-only-12345678";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = "ZetTechAvio",
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Настройка конвейера обработки HTTP-запросов
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Разрешает кросс-доменные запросы
app.UseCors("AllowReact");

// Перенаправление HTTP на HTTPS (только для Production)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Аутентификация
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Apply migrations automatically on startup
// DISABLED: Using SQL dump instead of EF migrations to preserve existing data
// try
// {
//     using (var scope = app.Services.CreateScope())
//     {
//         var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); 
//         db.Database.Migrate();
//     }
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"Migration error: {ex.Message}");
// }

// Явное слушание на всех интерфейсах для Docker
var urls = builder.Configuration["Urls"] ?? "http://0.0.0.0:5151;https://0.0.0.0:5152";
app.Run(urls);
