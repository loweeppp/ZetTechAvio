
## О проекте

Веб-приложение для поиска и бронирования авиабилетов. 
<img width="1849" height="1146" alt="изображение" src="https://github.com/user-attachments/assets/1a79c521-26be-4683-8804-0e0595377f97" />


## Технологии

Frontend: React, JavaScript, CSS, HTML

Backend: C#, ASP.NET Core 8.0, Entity Framework Core 

БД: MySQL 8.0

## Что реализовано

- Поиск и фильтрация рейсов по датам и маршрутам
- Регистрация и вход пользователей с JWT авторизацией
- Система тарифов для разных классов обслуживания (Economy, Business, First)
- Интеграция с YooKassa для обработки платежей
- Email подтверждение заказов с QR кодом билета
- Личный кабинет пользователя с историей бронирований
- HTTPS и безопасность (хеширование паролей, CORS)

## Структура кода

**Backend** (ZetTechAvio1.0/)
- Controllers/ - эндпоинты API
- Services/ - бизнес-логика
- Models/ - модели данных
- Data/ - работа с БД через EF Core

**Frontend** (frontend/src/)
- components/ - React компоненты (поиск, результаты, бронирование, профиль)
- pages/ - страницы приложения
- hooks/ - кастомные хуки для логики

## Основные сервисы

- **AuthenticationService** - регистрация, вход, JWT токены, управление паролями
- **FlightsService** - поиск, фильтрация и управление рейсами
- **BookingsService** - создание и управление бронированиями
- **FaresService** - управление тарифами для разных классов
- **PaymentService** - обработка платежей через YooKassa, webhook, email подтверждения
- **EmailService** - отправка писем с подтверждениями и QR кодами
- **NotificationService** - система уведомлений пользователей

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
GET    /api/auth/profile               - получить профиль текущего пользователя
```

**Рейсы:**
```
GET    /api/flights                    - получить все рейсы
GET    /api/flights/search             - поиск рейсов (параметры: from, to, date)
GET    /api/flights/{id}               - детали рейса
```

**Бронирования:**
```
POST   /api/bookings                   - создать новое бронирование
GET    /api/bookings/{id}              - получить детали бронирования
GET    /api/bookings/user              - мои бронирования
DELETE /api/bookings/{id}              - отменить бронирование
```

**Платежи:**
```
POST   /api/payments/create            - создать платеж в YooKassa
GET    /api/payments/{id}              - статус платежа
POST   /api/payments/webhook           - webhook от YooKassa (подтверждение платежа)
```

**Места в самолете:**
```
GET    /api/seats/{flightId}           - получить свободные места рейса
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

Проект демонстрирует практический опыт в разработке полнофункционального приложения: чистая архитектура, асинхронное программирование, DI контейнер, REST API, компонентный подход к UI, интеграция с внешними сервисами платежей.
