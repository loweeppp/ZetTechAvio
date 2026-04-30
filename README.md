
## О проекте

Веб-приложение для поиска и бронирования авиабилетов. 
<img width="1033" height="639" alt="изображение" src="https://github.com/user-attachments/assets/484ed731-486d-4342-b715-7a1d647b3080" />


## Технологии

Frontend: React, JavaScript, CSS, HTML

Backend: C#, ASP.NET Core 8.0, Entity Framework Core 

БД: MySQL 8.0

## Что реализовано

- **Интеллектуальный поиск рейсов** - AI парсер запросов на естественном языке (LLMService) для удобного поиска
- **Традиционный поиск** - форма с выбором города, даты и количества пассажиров
- **Поиск и фильтрация рейсов** - по датам, маршрутам, цене и времени вылета
- **Регистрация и вход** - JWT авторизация, защита паролей через bcrypt
- **Система тарифов** - разные классы обслуживания (Economy, Business, First) с разными ценами
- **Интеграция с YooKassa** - обработка платежей, webhook для подтверждения заказов
- **Email подтверждение** - отправка кодов подтверждения и QR кодов билетов
- **Личный кабинет** - история бронирований, управление профилем
- **HTTPS и безопасность** - SSL сертификат, CORS, валидация данных на backend и frontend

## Структура кода

**Backend** (project/ZetTechAvio1.0/)
- Controllers/ - API endpoints (Auth, Flights, Bookings, Payments, Fares, AI)
- Services/ - бизнес-логика (Authentication, Flights, Bookings, Payments, LLM)
- Models/ - модели данных (User, Flight, Booking, Fare и т.д.)
- Data/ - работа с БД через Entity Framework Core
- Migrations/ - миграции БД

**Frontend** (project/frontend/src/)
- pages/ - страницы приложения (HomePageV2, UserProfile, PrivacyPolicy)
  - homeV2/ - основная V2 страница с поиском и результатами
- features/ - модули по доменам
  - flights/ - поиск, AI парсер, результаты, фильтры
  - auth/ - модальные окна авторизации, профиля
  - bookings/ - модальное окно бронирования, история заказов
- components/ - переиспользуемые компоненты (HeaderV2, FooterV2)
- hooks/ - кастомные хуки (useAuth)

## Основные сервисы

- **AuthenticationService** - регистрация, вход, управление JWT токенами, хеширование паролей
- **JwtTokenService** - создание и валидация JWT токенов
- **PasswordHashingService** - хеширование и проверка паролей через bcrypt
- **FlightsService** - поиск, фильтрация рейсов, получение данных о рейсах
- **FaresService** - управление тарифами и ценами для разных классов обслуживания
- **BookingsService** - создание, отмена бронирований, управление данными заказов
- **LLMService** - парсинг текстовых запросов в параметры поиска (AI поиск)
- **PaymentService** - интеграция с YooKassa, обработка платежей и webhooks
- **UserValidationService** - валидация данных пользователя
- **AuthStateService** - управление состоянием авторизации на frontend

## БД

MySQL со следующими таблицами: Users, Flights, Bookings, Seats, Tickets, Aircraft, Airlines, Airports, Fares, Payments.

<img width="1033" height="833" alt="изображение" src="https://github.com/user-attachments/assets/f1e75e77-3d80-420a-be18-27a8da28601d" />

**Связи между таблицами:**
- Users 1:N Bookings (пользователь может иметь много бронирований)
- Flights 1:N Bookings (рейс может быть забронирован много раз)
- Flights 1:N Fares (один рейс имеет несколько классов)
- Flights 1:N Seats (рейс имеет много мест)
- Bookings 1:N Tickets (бронирование содержит много билетов)
- Bookings 1:1 Payments (каждое бронирование имеет один платеж)

---

## Доступ к приложению

**Production:**
- 🌐 URL: https://zettechavio.ru
- 🔐 HTTPS с SSL сертификатом
- 📱 Responsive дизайн (работает на мобильных устройствах)

---

## API

**Аутентификация:**
```
POST   /api/auth/register              - регистрация нового пользователя
POST   /api/auth/login                 - вход с email и паролем (возвращает JWT)
GET    /api/auth/current               - получить профиль текущего пользователя
POST   /api/auth/change                - изменить данные пользователя
POST   /api/auth/logout                - выход из системы
```

**Рейсы:**
```
GET    /api/flights                    - получить все рейсы
GET    /api/flights/search             - поиск рейсов (параметры: from, to, date)
GET    /api/flights/{id}               - детали рейса
GET    /api/flights/{id}/fares         - получить тарифы для рейса
```

**AI Поиск:**
```
POST   /api/ai/parse                   - парсить текстовый запрос в параметры поиска
```

**Бронирования:**
```
POST   /api/bookings                   - создать новое бронирование
GET    /api/bookings/{id}              - получить детали бронирования
GET    /api/bookings/my                - мои бронирования
DELETE /api/bookings/{id}              - отменить бронирование
POST   /api/bookings/request-confirmation - запросить код подтверждения
POST   /api/bookings/verify-code       - проверить код подтверждения
```

**Платежи:**
```
POST   /api/payment/create-payment     - создать платеж в YooKassa
GET    /api/payment/verify-status      - проверить статус платежа
POST   /api/payment/webhook            - webhook от YooKassa (подтверждение платежа)
```

**Тарифы:**
```
GET    /api/fares                      - получить все тарифы
GET    /api/fares/{id}                 - детали тарифа
```

---

## Безопасность

- 🔐 **JWT токены** - для защиты API endpoints
- 🔒 **Хеширование паролей** - с использованием bcrypt
- 🛡️ **HTTPS/TLS** - все данные передаются зашифрованными
-  **CORS** - правильная конфигурация для кросс-доменных запросов
- 📋 **Валидация данных** - на backend и frontend
- 🔑 **API ключи** - для YooKassa хранятся в user-secrets

---

## Развертывание

**Docker Compose:**
```bash
docker compose up -d
```

**Компоненты:**
- `zettechavio-db` - MySQL контейнер
- `zettechavio-backend` - ASP.NET Core API на порту 5151
- `zettechavio-frontend` - React приложение на порту 3000
- `zettechavio-nginx` - Nginx reverse proxy на портах 80 и 443

---

## О реализации

Полнофункциональное веб-приложение для поиска и бронирования авиабилетов, демонстрирующее:

**Backend:**
- Clean Architecture с разделением на Controllers, Services, Models, Data
- Асинхронное программирование (async/await)
- Dependency Injection контейнер для управления сервисами
- Entity Framework Core для работы с БД
- JWT для secure API authentication
- Интеграция с external API (YooKassa для платежей)
- LLM интеграция для парсинга текстовых запросов

**Frontend:**
- React 19 с новейшими возможностями
- React Router v7 для навигации
- Feature-based архитектура модулей
- Кастомные React хуки для логики
- Модальные диалоги для авторизации и бронирования
- Responsive дизайн с CSS3
- Client-side валидация данных
- Lucide React для иконок
