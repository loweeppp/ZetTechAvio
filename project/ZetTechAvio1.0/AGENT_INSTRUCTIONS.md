# ZetTechAvio - Инструкция для ИИ Агентов

## АРХИТЕКТУРА ПРОЕКТА

### Stack
- **Frontend**: React 18 + Hooks + CSS Modules
- **Backend**: ASP.NET Core 9 + Entity Framework Core
- **Database**: MySQL 8.0
- **Auth**: JWT токены + localStorage
- **API**: REST с Content-Type: application/json

### Структура папок
```
ZetTechAvio1.0/
├── Controllers/        # API endpoints
├── Services/           # Бизнес-логика
├── Models/             # Entities + DTOs
├── Data/               # DbContext
├── Migrations/         # EF миграции
├── Components/         # Razor (лишнее)
└── Program.cs          # DI + конфиг

frontend/
├── src/
│   ├── components/     # React компоненты
│   ├── hooks/          # Custom hooks (useAuth)
│   ├── pages/          # Страницы
│   └── App.js
```

## ГЛАВНЫЕ КОМПОНЕНТЫ

### Models (они же Entity)
1. **User** - пользователи (авторизация)
2. **Flight** - рейсы (основной entity)
3. **Fare** - тарифы (Economy/Business/First)
4. **Booking** - бронирования (сохраняет заказ)
5. **Ticket** - билеты (один билет на пассажира в бронировании)
6. **Seat** - места в самолёте (future use)

**关键关系:**
- Flight → Fares (один ко многим)
- Booking → Tickets (один ко многим)
- Ticket → Fare + Flight + Seat

### Services (Бизнес-логика)
- **AuthenticationService** - login/register/change password
- **FlightsService** - получение рейсов, поиск, DTO маппинг
- **BookingsService** - создание бронирования, история
- **UserValidationService** - валидация данных
- **PasswordHashingService** - хеширование

### Controllers (API)
- **AuthController** - POST /api/auth/login, /register, /change
- **FlightsController** - GET /api/flights, /search, /{id}/fares
- **BookingsController** - POST /api/bookings, GET /{userId}

### Frontend Components
- **Header** - навигация + кнопки auth
- **FlightsList** - список рейсов с фильтром
- **BookingModal** - модal для бронирования
- **AuthModal** - login/register
- **ProfileModal** - профиль пользователя

### Custom Hooks
- **useAuth()** - управление авторизацией (currentUser, login, logout, changeUser)

## РАБОЧИЕ ПОТОКИ

### 1. ПОИСК И ПРОСМОТР РЕЙСОВ
```
SearchForm (onChange) → Home.jsx (searchParams state)
→ FlightsList (useEffect + fetch /api/flights или /api/flights/search)
→ FlightDto[] (уже с minPrice посчитана на бэке)
→ Отображение в карточках
```

**Важно:**
- Query params используют URLSearchParams
- тарифы хранят enum как число (0=Economy, 1=Business, 2=First)
- На фронте нужно маппировать через CLASS_NAMES

### 2. АУТЕНТИФИКАЦИЯ
```
AuthModal (форма) → POST /api/auth/login
← JWT токен + User объект
→ localStorage.setItem('token')
→ useAuth hook обновляет currentUser
→ Header показывает имя пользователя
```

**Настройки:**
- Token хранится в localStorage
- Отправляется в header: `Authorization: Bearer ${token}`
- Пароли хешируются BCrypt-подобным методом

### 3. БРОНИРОВАНИЕ
```
Нажимаем "Купить" на рейсе
→ BookingModal открывается
→ Загружаются тарифы GET /api/flights/{id}/fares
→ Выбираем класс + кол-во + заполняем пассажиров
→ POST /api/bookings
→ Создаётся Booking + Ticket для каждого пассажира
← Номер бронирования (PNR) возвращается
```

**Payload:**
```json
{
  "flightId": 5,
  "fareId": 10,
  "quantity": 2,
  "passengers": [
    {
      "fullName": "Иван Иванов",
      "passengerType": "Adult",
      "passportSeries": "XX",
      "passportNumber": "123456"
    }
  ]
}
```

## ГЛАВНЫЕ ПАТТЕРНЫ

### DTO Маппинг (бэк)
```csharp
// FlightDto не содержит Fares - только готовый minPrice
private FlightDto MapToDto(Flight flight)
{
    return new FlightDto
    {
        Id = flight.Id,
        MinPrice = flight.Fares.Any() ? flight.Fares.Min(f => f.Price) : 0,
        // ... остальные поля
    };
}

// В Service возвращаем DTO, не Entity
public async Task<List<FlightDto>> GetAllFlightsAsync()
{
    var flights = await _dbContext.Flights
        .Include(f => f.OriginAirport)
        .Include(f => f.DestAirport)
        .Include(f => f.Fares)
        .ToListAsync();
    
    return flights.Select(MapToDto).ToList();
}
```

### Валидация
```csharp
// Split на registration vs update
public async Task<bool> ValidateRegistrationAsync(string email, string password)
{
    // Email должен быть уникален
    // Пароль >= 6 символов
}

public async Task<bool> ValidateUpdateAsync(int userId, string email, string password)
{
    // Email уникален ДО СКОЛЬКУ НЕ текущего пользователя
    if (await _dbContext.Users.AnyAsync(u => u.Email == email && u.Id != userId))
        return false;
}
```

### Enum и Conversion
```csharp
// В модели используем enum
public enum BookingStatus { Created, Paid, Confirmed, Cancelled }

// В миграции конвертимся через string
entity.Property(e => e.Status)
    .IsRequired()
    .HasConversion<string>();  // ← автоматически конвертирует
```

### Связи между Entity
```csharp
// One-to-Many: Flight → Fares
modelBuilder.Entity<Flight>()
    // ... другие свойства
    
modelBuilder.Entity<Fare>()
    .HasOne(f => f.Flight)
    .WithMany(fl => fl.Fares)
    .HasForeignKey(f => f.FlightId)
    .OnDelete(DeleteBehavior.Cascade);

// Many-to-Many опционально через junction table
```

## КАКИЕ ОШИБКИ ЧИНИЛИ

### 1. FlightId1 shadow property
**Проблема:** EF создавал второй FK `FlightId1` из-за неправильной конфигурации
**Решение:** В OnModelCreating используй явную конфигурацию:
```csharp
.HasOne(f => f.Flight)
.WithMany(fl => fl.Fares)  // ← важно указать навигатор обратно
.HasForeignKey(f => f.FlightId)
```

### 2. Enum как число при сериализации
**Проблема:** fare.class приходит в JSON как `0, 1, 2` вместо строк
**Решение:** На фронте маппируй через dict:
```javascript
const CLASS_NAMES = { 0: 'Economy', 1: 'Business', 2: 'First' };
CLASS_NAMES[fare.class]  // → 'Economy'
```

### 3. Required properties в новых Entity
**Проблема:** CS9035 - `required Flight Flight` без значения
**Решение:** Сделай nullable:
```csharp
public Flight? Flight { get; set; }  // ← вместо required
```

### 4. ValidationException при профиль эдит
**Проблема:** "Email already registered" при редактировании своего профиля
**Решение:** Исключи текущего пользователя из проверки:
```csharp
where u.Email == email && u.Id != userId
```

## ЗАПУСК И ДЕБАГ

### Запуск
```bash
# Terminal 1 - Backend
cd ZetTechAvio1.0
dotnet watch run

# Terminal 2 - Frontend  
cd frontend
npm start
```

### Полезные команды
```bash
# Создать миграцию
dotnet ef migrations add AddNewTable

# Применить миграцию
dotnet ef database update

# Откатить миграцию
dotnet ef migrations remove

# Посмотреть SQL
dotnet ef migrations script
```

### DevTools проверки
1. **Network** - смотри запросы на /api/* - ищи статус 400/500
2. **Console** - JSON.parse ошибки
3. **LocalStorage** - есть ли token, какой value
4. **Application** - cookies чистые

## СЛЕДУЮЩИЕ ФИЧИ (TODO)

- [ ] Booking history на фронте
- [ ] Отмена бронирования (refund logic)
- [ ] Выбор мест (seat selection)
- [ ] Payment system интеграция
- [ ] Admin panel для редактирования рейсов
- [ ] Email подтверждение бронирования
- [ ] Фильтр по цене/авиакомпании
- [ ] Сортировка результатов

## КОНВЕНЦИИ КОДА

### Naming
- Controllers: `{Entity}Controller.cs`
- Services: `{Entity}Service.cs` + интерфейс `I{Entity}Service`
- Models: `{EntityName}.cs` (Flight, Booking, etc)
- DTOs: `{EntityName}Dto.cs` или включены в Models файл

### API Endpoints
- GET    `/api/{resource}` - получить все
- GET    `/api/{resource}/{id}` - получить одну
- POST   `/api/{resource}` - создать
- PUT    `/api/{resource}/{id}` - обновить
- DELETE `/api/{resource}/{id}` - удалить

### Error Handling
```csharp
try
{
    // логика
}
catch (InvalidOperationException ex)
{
    _logger.LogError($"...: {ex.Message}");
    return BadRequest(new { message = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError($"Unexpected: {ex.Message}");
    return StatusCode(500, new { message = "Internal server error" });
}
```

## БД SCHEMA

### Таблицы
- **Users** - email, password_hash, full_name, phone, role
- **Flights** - flight_number, airline_id, aircraft_id, origin_airport_id, dest_airport_id, departure_dt, arrival_dt, duration_minutes, status
- **Fares** - flight_id, fare_class (Economy/Business/First), price, seats_available, baggage_included
- **Bookings** - user_id, booking_reference (PNR), total_amount, status, created_at
- **Tickets** - booking_id, flight_id, fare_id, seat_id, ticket_number, passenger_name, passenger_type, passport_series, passport_number, price, status
- **Seats** - flight_id, seat_number, seat_class, status (future use)

## ИНСТРУКЦИЯ ДЛЯ АГЕНТОВ

Если ты ИИ агент и работаешь над ZetTechAvio:

1. **Перед тем как делать changes** - прочитай этот файл + посмотри на текущий код
2. **Для добавления новой фичи:**
   - Добавь Model (Entity)
   - Добавь DbSet в ApplicationDbContext
   - Создай миграцию: `dotnet ef migrations add FeatureName`
   - Примени: `dotnet ef database update`
   - Создай Service + Interface
   - Зарегистрируй в Program.cs: `builder.Services.AddScoped<IFeatureService, FeatureService>()`
   - Создай Controller с endpoints
   - На фронте создай компонент + hook если нужно

3. **Для баг-фиксов:**
   - Посмотри ошибку в console/network
   - Проверь на фронте: есть ли token, правильные ли параметры
   - Проверь на бэке: логика, валидация, DB queries
   - Используй DevTools для дебага

4. **Code Review перед commit:**
   - Компилируется ли: `dotnet build`
   - Нет ли неиспользуемых using
   - Есть ли try-catch где нужно
   - Nullable types где надо (для nullable БД полей)

5. **Если сломалось:**
   - Откати последнюю миграцию: `dotnet ef database update PreviousMigration`
   - Удали File, который создал
   - Начни заново

---

**Дата создания:** 27 февраля 2026
**Версия:** 1.0 (Поиск рейсов + Авторизация + Бронирование)
**Автор проекта:** Кирилл (диплом ZetTechAvio)
