# ZetTechAvio - Система бронирования авиабилетов

![ZetTechAvio](./preview.jpg)

## О проекте

Веб-приложение для поиска и бронирования авиабилетов. Полный стек: React на фронте, ASP.NET Core на бэке.

## Технологии

Frontend: React, JavaScript, CSS  
Backend: ASP.NET Core 8.0, Entity Framework Core  
БД: MySQL 8.0

## Что реализовано

- Поиск и фильтрация рейсов по датам и маршрутам
- Регистрация и вход пользователей с JWT авторизацией
- Бронирование билетов с выбором мест
- Управление профилем и историей бронирований
- Система тарифов для разных классов билетов
- Email подтверждение заказов
- Адаптивный интерфейс

## Быстрый старт

Frontend:
```bash
cd frontend
npm install
npm start
```

Backend:
```bash
cd ZetTechAvio1.0
dotnet restore
dotnet ef database update
dotnet run
```

Frontend: http://localhost:3000  
API: http://localhost:5151

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

- AuthenticationService - регистрация, вход, JWT токены
- FlightsService - поиск и фильтрация рейсов
- BookingsService - создание и управление бронированиями
- FaresService - работа с тарифами
- ConfirmationService - отправка писем с подтверждениями

## БД

MySQL со следующими таблицами: Users, Flights, Bookings, Seats, Tickets, Aircraft, Airlines, Airports, Fares, ConfirmationCodes.

## API

```
POST /api/auth/register
POST /api/auth/login
GET  /api/flights
GET  /api/flights/search?from=...&to=...&date=...
POST /api/bookings
GET  /api/bookings/user/{id}
```

## О реализации

Проект демонстрирует практический опыт в разработке полнофункционального приложения: чистая архитектура, асинхронное ПО, DI контейнер, REST API, компонентный подход к UI.