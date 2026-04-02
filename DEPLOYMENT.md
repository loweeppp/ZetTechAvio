# Развертывание ZetTechAvio на VPS с Docker

## Подготовка

### 1. Установка Docker и Docker Compose на Ubuntu 24.04

```bash
# Обнови систему
sudo apt update && sudo apt upgrade -y

# Установи Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Добавь текущего пользователя в группу docker
sudo usermod -aG docker $USER
newgrp docker

# Проверка
docker --version
docker-compose --version
```

### 2. Клонировани/загрузка проекта на VPS

```bash
# Через WinSCP или git
# Если используешь git:
git clone https://github.com/твой-гитхаб/ZetTechAvio.git
cd ZetTechAvio
```

### 3. Настройка DNS (очень важно!)

Зайди в панель управления доменом zettechavio.ru и добавь A-запись:
```
Type: A
Name: @ (или оставить пусто)
Value: ВАШ_IP_VPS
TTL: 3600
```

И для API поддомена:
```
Type: A
Name: api
Value: ВАШ_IP_VPS
TTL: 3600
```

Или если используешь CNAME:
```
Type: CNAME
Name: api
Value: zettechavio.ru
TTL: 3600
```

Подожди 15-30 минут чтобы DNS пропогировался.

---

## Развертывание

### 1. Подготовка переменных кода

Открой файл `.env` (если его нет - создай):

```bash
nano .env
```

Добавь:
```
DB_PASSWORD=12344%&J
ASPNETCORE_ENVIRONMENT=Production
```

Сохрани: Ctrl+X → Y → Enter

### 2. Обновление appsettings на сервере

В файле `project/ZetTechAvio1.0/appsettings.json` убедись что:
- ConnectionString указывает на `mysql` (имя сервиса в docker-compose)
- CORS содержит твой домен

### 3. Запуск Docker Compose

```bash
# Перейди в корневую папку проекта
cd ~/ZetTechAvio

# Построй образы Congratulations! Your certificate is ready.
Path to certificate: /etc/letsencrypt/live/zettechavio.ru/fullchain.pem
Path to key: /etc/letsencrypt/live/zettechavio.ru/privkey.pem
docker-compose up -d

# Проверь статус
docker-compose ps

# Логи (если что-то не работает)
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f mysql
```

### 4. Создание базы данных

```bash
#Теперь применим миграции БД:
docker compose exec backend dotnet ef database update

```


### 5. Проверка работы

```bash
# HTTP пока работает
curl http://ТВОЙ_IP_VPS/
curl http://ТВОЙ_IP_VPS:5151/api/flights

# После пропагирования DNS
curl http://zettechavio.ru/
curl http://api.zettechavio.ru/api/flights
```

---

## SSL/HTTPS (Let's Encrypt)

### Проверка поротов vps

# ============================================
# 1. Проверить какие порты слушают
# ============================================
sudo ss -tlnp

# Должны видеть:
# LISTEN 0.0.0.0:80          (nginx)
# LISTEN 0.0.0.0:443         (nginx)
# LISTEN 0.0.0.0:3306        (MySQL в Docker)
# LISTEN 0.0.0.0:5151        (Backend в Docker)


# ============================================
# 2. Проверить состояние UFW (если включен)
# ============================================
sudo ufw status

# Ответ: "Status: inactive" или "Status: active"


# ============================================
# 3. Если нужно открыть порты (если UFW active)
# ============================================
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw reload

### Установка Certbot


```bash
# Останавливаем контейнеры
cd /root/ZetTechAvio
docker compose down

# Установка certbot
sudo apt-get update
sudo apt-get install -y certbot

# Получение сертификата (используем standalone режим)
sudo certbot certonly --standalone -d zettechavio.ru -d www.zettechavio.ru -d api.zettechavio.ru --non-interactive --agree-tos -m admin@zettechavio.ru
```
```bash
# Проверить DNS
nslookup zettechavio.ru
nslookup zettechavio.ru -debug
nslookup zettechavio.ru 8.8.8.8

```

Альтернатива: используйте самоподписанный сертификат (временный) для Docker:

```bash
mkdir -p /root/ZetTechAvio/ssl

sudo certbot certonly --standalone -d zettechavio.ru -d www.zettechavio.ru -d api.zettechavio.ru

sudo chown -R $(id -u):$(id -g) /root/ZetTechAvio/ssl/
ls -la /root/ZetTechAvio/ssl/
```

<!-- Сертификаты будут в `/etc/letsencrypt/live/zettechavio.ru/` -->

<!-- ### Копирование сертификатов внутрь Docker

```bash
# Создай папку для SSL
mkdir -p ~/ZetTechAvio/ssl

# Скопируй сертификаты
sudo cp /etc/letsencrypt/live/zettechavio.ru/fullchain.pem ~/ZetTechAvio/ssl/cert.pem
sudo cp /etc/letsencrypt/live/zettechavio.ru/privkey.pem ~/ZetTechAvio/ssl/key.pem

# Дай права
sudo chown $USER:$USER ~/ZetTechAvio/ssl/*
``` -->

### Обнови nginx.conf

Раскомментируй HTTPS блоки в файле `nginx.conf` и закомментируй HTTP блоки для редиректа на HTTPS.

Перезагрузи Nginx:
```bash
docker exec zettechavio-nginx nginx -s reload
```

### Автоматическое обновление сертификатов

```bash
# Настрой cronJob для автообновления Certbot
sudo systemctl enable certbot.timer
sudo systemctl start certbot.timer
```

---

## Управление

### Остановка всех сервисов

```bash
docker-compose down
```

### Просмотр логов

```bash
docker-compose logs -f
docker-compose logs -f backend
docker-compose logs -f mysql
```

### Перестройка образов (если изменился код)

```bash
docker-compose up -d --build
```

### Очистка Docker

```bash
docker-compose down -v  # с удалением volumes
docker system prune     # удалить неиспользуемые образы
```

---

## Проблемы и решения

### Frontend не загружается?
```bash
docker logs zettechavio-frontend
# Проверь nginx.conf в frontend контейнере
```

### Backend не подключается к БД?
```bash
# Проверь что MySQL запущен и здоров
docker-compose ps

# Посмотри логи БД
docker-compose logs mysql
```

### Порты занят?
```bash
# Найди процесс на порту 80
sudo lsof -i :80
sudo kill -9 ПИД_НОМЕР
```

### DNS не пропагировался?
```bash
# Проверь DNS
nslookup zettechavio.ru
dig zettechavio.ru
```

---

## Production чеклист

- [x] Docker Compose настроен
- [x] Переменные окружения установлены  
- [x] DNS указывает на VPS
- [ ] SSL сертификат установлен
- [ ] Автообновление сертификата настроено
- [ ] Backup БД настроен
- [ ] Логирование настроено
- [ ] Monitoring настроен?

---

## Полезные команды

```bash
# Проверка IP VPS
curl ifconfig.me

# Проверка портов открыты ли
nc -zv ТВОЙ_IP 80
nc -zv ТВОЙ_IP 443

# Перезагрузка всех сервисов
docker-compose restart

# Удаление контейнера (осторожно!)
docker-compose rm backend

# Размер volumes
docker exec zettechavio-mysql du -sh /var/lib/mysql
```
