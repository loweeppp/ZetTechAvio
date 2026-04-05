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
builder.Services.AddScoped<IConfirmationService, ConfirmationService>();
builder.Services.AddScoped<IConfirmationService>(sp => 
    new ConfirmationService(
    sp.GetRequiredService<ApplicationDbContext>(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<IWebHostEnvironment>()));
        //  builder.Configuration));


// Конфигурация HttpClient
builder.Services.AddHttpClient();
// Настройка HttpClient в зависимости от среды
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped(sp =>
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5151") };
    });
}
else
{
    builder.Services.AddScoped(sp => 
        new HttpClient { BaseAddress = new Uri(builder.Configuration["HttpClient:BaseAddress"] ?? "http://localhost:5151") }
    );
}

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
});

// Добавляем базовую схему аутентификации
builder.Services.AddAuthentication()
    .AddCookie("Identity.Application");

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
