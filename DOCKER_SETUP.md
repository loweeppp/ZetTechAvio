# Docker Deployment Summary for ZetTechAvio

## Цель проекта

Развернуть полнофункциональное приложение ZetTechAvio (авиабронирование) на production VPS с использованием Docker. Приложение включает:
- **Frontend:** React приложение на порту 3000
- **Backend:** ASP.NET Core API на порту 5151
- **БД:** MySQL 5.7 на порту 3306
- **Reverse Proxy:** Nginx для маршрутизации трафика по доменам

**Домены:**
- `zettechavio.ru` → Frontend (React)
- `api.zettechavio.ru` → Backend (ASP.NET API)

---

## История разработки и проблемы

### Этап 1: Cloudflare Tunnel (Основной вариант)
- ❌ Подход отброшен: требовал постоянно запущенный cloudflared на локальной машине
- Переход на direct VPS deployment

### Этап 2: Первая VPS попытка (10GB)
- ❌ Проблема: диск переполнился (MySQL 8.0 высокое потребление)
- ❌ Решение: обновление до 20GB VPS

### Этап 3: Frontend npm install в Docker
- ❌ Проблема #1: npm fetch таймауты при скачивании пакетов (ETIMEDOUT)
  - Решение: загрузить `build/` локально, затем скопировать в Docker
  - Результат: сборка 5 минут вместо 30 минут восстановления сети
  
- ❌ Проблема #2: `.dockerignore` исключал `build/` папку
  - Решение: удалить `build/` из `.dockerignore`

### Этап 4: Backend БД подключение
- ❌ Проблема: "SSL Authentication Error" при старте контейнера
  - Root cause: MySQL 5.7 требует SSL, backend пытался подключиться с SSL
  - Решение: добавить `SslMode=none` в ConnectionString docker-compose.yml

- ✅ Добавлена автоматическая миграция БД в Program.cs:
  ```csharp
  using (var scope = app.Services.CreateScope()) {
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      db.Database.Migrate();
  }
  ```

### Этап 5: Nginx HTTPS и SSL
- ❌ Проблема #1: Let's Encrypt certbot — порты 80/443 закрыты firewall LandVPS
  - Решение: создать self-signed сертификат для тестирования (возможно придётся пересоздать ndinx для нового сертификата)

- ❌ Проблема #2: Nginx синтаксис `listen 443 ssl http2` устарел
  - Решение: разделить на `listen 443 ssl;` и отдельную директиву `http2 on;`

- ❌ Проблема #3: Порт 443 занят Amnezia VPN
  - Решение: временно остановить VPN контейнеры для запуска nginx

- ❌ Проблема #4: Let's Encrypt HTTP-01/Webroot challenge — таймауты
  - Root cause: LandVPS блокирует исходящий IPv4 трафик на 443, блокирует входящий на 80 для некоторых операций
  - Решение #1 (Не сработало): Certbot `--standalone` и `--webroot` — оба требуют доступности http://, которая недоступна
  - Решение #2 (Не полностью): DNS-01 через certbot-dns-regruru — требует pip3 install в externally-managed-environment (Ubuntu 24.04)
  - **Финальное решение:** Использовать self-signed сертификаты как постоянное решение
    - Self-signed достаточны для API, mobile apps, и backend-to-backend коммуникации
    - Браузер покажет warning, но это приемлемо для staging/dev
    - Для production DNS-01 через Cloudflare (если переедете на их nameservers)

- ❌ Проблема #5 Nginx не может резолвить имена контейнеров (frontend, backend)
  - Root cause: Nginx парсит конфиг на старте и пытается резолвить DNS до que контейнеры готовы
  - **Решение:** Использовать переменные в `proxy_pass` для отложенной DNS резолюции:
    ```nginx
    set $upstream_frontend "frontend:80";
    proxy_pass http://$upstream_frontend;
    
    set $upstream_backend "backend:5151";
    proxy_pass http://$upstream_backend;
    ```
  - Добавлен `resolver 127.0.0.11:53` в http блок для Docker встроенного DNS

- ❌ Проблема #6: Amnezia-xray контейнер "как зомби" занимает порт 443
  - Root cause: Amnezia контейнер остался запущенным даже после `docker-compose down`
  - Решение:
    ```bash
    docker stop amnezia-xray amnezia-awg
    docker rm amnezia-xray amnezia-awg
    ```
  - Или если в отдельном docker-compose: `docker-compose -f amnezia/docker-compose.yml down`



### Этап 6: DNS маршрутизация
- ✅ Настроены A-рекорды в reg.ru:
  - `@` (зettechavio.ru) → 193.163.170.82
  - `www` → 193.163.170.82
  - `api` → 193.163.170.82

---

## Что создано

### 1. **Dockerfile'ы**
- ✅ `project/ZetTechAvio1.0/Dockerfile` — мультиступенчатая сборка .NET (SDK → runtime)
- ✅ `project/frontend/Dockerfile` — Node.js сборка React → Nginx
- ✅ Оба используют Alpine базовый образ для минимального размера

### 2. **Docker Compose**
- ✅ `docker-compose.yml` — оркестрация 4 сервисов:
  - `mysql:5.7` — БД с healthcheck
  - `zettechavio-backend` — ASP.NET Core на 0.0.0.0:5151
  - `zettechavio-frontend` — React + Nginx
  - `zettechavio-nginx` — reverse proxy с SSL

### 3. **Конфигурация Nginx**
- ✅ `nginx.conf` — два SSL блока:
  - `:443` для zettechavio.ru/www → frontend:80
  - `:443` для api.zettechavio.ru → backend:5151
- ✅ HTTP→HTTPS редирект
- ✅ Self-signed SSL сертификаты в `/ssl` volume

### 4. **Code Updates**
- ✅ `Program.cs` — слушание на 0.0.0.0:5151 (требуется для Docker)
- ✅ `Program.cs` — автоматическое применение миграций при старте
- ✅ `appsettings.json` — динамическая конфигурация CORS и HttpClient

---

## Текущее состояние (✅ Работает)

### Все контейнеры Up
```
mysql           — Connected (port 3306)
backend         — Running (port 5151, БД готова)
frontend        — Running (port 3000)
nginx           — Running (ports 80, 443)
```

### API endpoints работают
```bash
curl -k -H "Host: zettechavio.ru" https://localhost/
    ↓ Returns: React HTML (200 OK)

curl -k -H "Host: api.zettechavio.ru" https://localhost/api/flights
    ↓ Returns: [] (200 OK)
```

### БД инициализирована
- ✅ Все миграции применены
- ✅ Таблицы созданы: Users, Flights, Bookings, Fares, ConfirmationCodes и т.д.
- ✅ Данные БД персистируются в volume

---

## Будущие планы

### 🔴 Критические
1. **HTTPS Production сертификаты (Let's Encrypt через DNS-01)**
   - Откройте порты 80/443 в firewall LandVPS (уже)
   - Запустите: `sudo certbot certonly --standalone -d zettechavio.ru -d api.zettechavio.ru`
   - Скопируйте `/etc/letsencrypt/live/zettechavio.ru/` → `/root/ZetTechAvio/ssl/`

2. **Frontend/Backend конфигурация API базового адреса**
   - Обновить API_URL 

### 🟡 Важные

1. **YooKassa интеграция платежей**
   - Реализовать checkout flow с YooKassa API
   - Обновить backend контроллер BookingsController

2. **Email подтверждение** (проверить)
   - Настроить SMTP (mail.ru) для отправки confirmation codes
   - Протестировать booking → email workflow

### 🟢 Желательные
7. **Мониторинг и логирование**
   - Добавить ELK stack (Elasticsearch, Logstash, Kibana)
   - Или использовать простой solution: Loki + Grafana

8. **Backup БД**
   - Автоматический ежедневный backup в хранилище (S3, Google Drive)
   - Script: `mysqldump` в cronjob

9. **Auto-renewal SSL**
   - Настроить certbot renewal: `sudo systemctl enable certbot.timer`
   - Или использовать Let's Encrypt через Docker (traefik)

10. **CI/CD Pipeline**
    - GitHub Actions для автоматического build → deploy
    - На коммит → docker build → push на registry → VPS автоматически обновляется

---

## Команды для ежедневной работы

### Просмотр логов
```bash
docker-compose logs -f                    # Все логи
docker-compose logs -f backend            # Только backend
docker-compose logs -f nginx              # Только nginx
```

### Перезагрузка сервиса
```bash
docker-compose restart backend            # Перезагрузить backend
docker-compose down && docker-compose up -d  # Full restart
```

### Проверка здоровья
```bash
docker-compose ps                         # Статус всех контейнеров
curl -k https://zettechavio.ru/           # Проверить frontend
curl -k https://api.zettechavio.ru/       # Проверить backend
```

### Обновление кода
```bash
cd /root/ZetTechAvio
git pull
docker-compose build --no-cache backend   # Пересобрать backend если изменился
docker-compose up -d                      # Переобновить
```

---

## Важные файлы и их расположение

| Файл | Путь | Описание |
|------|------|---------|
| docker-compose.yml | `/root/ZetTechAvio/` | Оркестрация сервисов |
| nginx.conf | `/root/ZetTechAvio/` | Конфигурация reverse proxy |
| SSL сертификаты | `/root/ZetTechAvio/ssl/` | fullchain.pem + privkey.pem |
| БД данные | Docker volume `mysql_data` | Персистентное хранилище |
| Логи | `docker-compose logs` | В stdout |

---

## Архитектура финальная

```
Internet (193.163.170.82:80, :443)
    ↓
[Nginx Container - Reverse Proxy]
    ├─ Host: zettechavio.ru → docker-frontend:80 (React SPA)
    └─ Host: api.zettechavio.ru → docker-backend:5151 (API)

[Docker Network - zettechavio-network]
    ├─ Frontend (nginx alpine serving React build)
    ├─ Backend (ASP.NET Core listening 0.0.0.0:5151)
    └─ MySQL (5.7 БД, volume mysql_data)

[SSL: Self-signed for dev, Let's Encrypt for prod]
```

---

## Что работает на текущий момент (31 марта 2026)

✅ **Full stack deployed на VPS**
- Frontend доступна через nginx (HTTPS selbst-signed)
- Backend API отвечает на запросы (/api/flights → [])
- БД инициализирована
- Docker orchestration работает

⚠️ **Требует внимания**
- SSL cert: используется self-signed (требуется Let's Encrypt через DNS-01 для production)
- Frontend/Backend конфиг: API_URL потребуеться обновления
- SECURITY.md: Требуеться полноя замена всех логинов как сказано в файле .md

❌ **Не реализовано**
- Payment integration (YooKassa)
- Email confirmations (SMTP), (работает, есть недочет того что имеллось ввиду под "Email confirmations SMTP" )
- Initial data seeding (airlines, flights)
- Production SSL сертификаты

---

## dven Для следующего API агента

Если нужно продолжить работу:
1. SSH: `ssh root@193.163.170.82`
2. Перейти: `cd /root/ZetTechAvio && docker-compose ps`
3. Смотреть логи: `docker-compose logs -f backend`
4. Если изменяете код — обновляйте на VPS и перестраивайте контейнеры

**Все основное работает. Остаток — это бизнес-логика и API интеграции.**
