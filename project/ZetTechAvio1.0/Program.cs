using ZetTechAvio1._0.Components;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Database=ZetTechAvioDB;User=root;Password=;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, 
        ServerVersion.AutoDetect(connectionString)));

// Регистрация сервисов
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();
builder.Services.AddScoped<IUserValidationService, UserValidationService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IAuthStateService, AuthStateService>();
builder.Services.AddScoped<IFaresService, FaresService>();


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

builder.Services.AddScoped<IFlightsService, FlightsService>();

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
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Настройка конвейера обработки HTTP-запросов
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//Перенаправление HTTP на HTTPS
app.UseHttpsRedirection();
//Разрешает кросс-доменные запросы
app.UseCors("AllowReact");
//Аутентификация
app.UseAuthentication();

app.MapControllers();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
