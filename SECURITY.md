# 🔒 Security Guide - ZetTechAvio

## ✅ Security Status (актуально на 2 апреля 2026)

### Реализовано:
- ✅ `.env` конфигурация (секреты не в коде)
- ✅ `.gitignore` защищает `.env`
- ✅ `docker-compose.yml` использует переменные окружения
- ✅ GitHub Secrets для CI/CD (VPS_HOST, DOCKER_UP)
- ✅ SSH ключи для деплоя на VPS
- ✅ Все пароли удалены из кода

### Обнаруженные уязвимости:

```
❌ docker-compose.yml
   └─ MYSQL_ROOT_PASSWORD=12344%&J (ВИДНА)
   └─ ConnectionStrings__DefaultConnection с паролем (ВИДНА)

❌ appsettings.json (если в репо)
   └─ Все конфиги видны

❌ Program.cs
   └─ HttpClient BaseAddress может содержать токены
   └─ Пример-пароли в комментариях

❌ Frontend App.js
   └─ API_URL может содержать базовую авторизацию
```


---

## 🚨 СРОЧНЫЕ ДЕЙСТВИЯ (ПРЯМО СЕЙЧАС)

### 1. Смените ВСЕ пароли (Google кэш может видеть):

**На VPS — смените MySQL пароль:**
```bash
docker exec -it zettechavio-mysql mysql -u root -p12344%&J

mysql> ALTER USER 'root'@'%' IDENTIFIED BY 'NewSecurePassword!@#2026';
mysql> ALTER USER 'root'@'localhost' IDENTIFIED BY 'NewSecurePassword!@#2026';
mysql> FLUSH PRIVILEGES;
mysql> EXIT;
```

**Обновите docker-compose.yml на VPS:**
```bash
# На локальной машине
# Редактируйте docker-compose.yml заменив 12344%&J на новый пароль

# Затем:
scp docker-compose.yml root@193.163.170.82:/root/ZetTechAvio/

# На VPS:
cd /root/ZetTechAvio
docker-compose down
docker-compose up -d
```

### 2. Переставьте YooKassa, Mail.ru, другие API ключи:
- Войдите в каждый сервис
- Invalidate/delete старые ключи
- Создайте новые

### 3. Очистите Git историю:

```bash
# Проверьте что уже expose в истории:
git log --all -S "12344" --oneline
git log --all -S "password" --oneline

# Если найдено — переписать историю:
git filter-repo --replace-text <(echo "12344%&J==>REDACTED")
git push origin main --force-with-lease  # Опасно! Скажет другим перепуллить
```

---

## ✅ FIX: Правильная конфигурация

### Шаг 1: Создайте `.env.example` (шаблон БЕЗ реальных значений)

**`.env.example:`**
```env
# ============================================
# 🔐 ENVIRONMENT VARIABLES TEMPLATE
# ============================================
# Copy this file to .env and fill with REAL values
# NEVER commit .env file to git!

# ============================================
# Database
# ============================================
MYSQL_ROOT_PASSWORD=CHANGE_THIS_IN_PRODUCTION
MYSQL_DATABASE=ZetTechAvioDB
DB_PASSWORD=CHANGE_THIS_IN_PRODUCTION

# ============================================
# ASP.NET Core Backend
# ============================================
ASPNETCORE_ENVIRONMENT=Production
Urls=http://0.0.0.0:5151

# ============================================
# HTTP Client Configuration
# ============================================
HttpClient__BaseAddress=https://api.zettechavio.ru

# ============================================
# CORS Configuration
# ============================================
Cors__AllowedOrigins=https://zettechavio.ru;https://www.zettechavio.ru

# ============================================
# Security Secrets (GENERATE NEW VALUES!)
# ============================================
JWT_SECRET=GENERATE_RANDOM_256_BIT_HEX_STRING_HERE
JWT_ISSUER=https://zettechavio.ru
JWT_AUDIENCE=https://zettechavio.ru

# ============================================
# Payment Integration (YooKassa)
# ============================================
YOOKASSA_SHOP_ID=YOUR_SHOP_ID_HERE
YOOKASSA_API_KEY=YOUR_API_KEY_HERE
YOOKASSA_RETURN_URL=https://zettechavio.ru/bookings

# ============================================
# Email Configuration (SMTP)
# ============================================
SMTP_HOST=smtp.mail.ru
SMTP_PORT=465
SMTP_USER=your-email@mail.ru
SMTP_PASSWORD=YOUR_APP_PASSWORD_HERE
SMTP_FROM=noreply@zettechavio.ru

# ============================================
# Database Backup
# ============================================
BACKUP_SCHEDULE=0 2 * * *  # Every day at 2 AM

# ============================================
# Logging
# ============================================
LOG_LEVEL=Information
```

### Шаг 2: Обновите `.gitignore`

**`.gitignore` (добавьте в конец):**
```gitignore
# ============================================
# 🔐 SECURITY - Never commit secrets
# ============================================
.env
.env.local
.env.*.local
.env.production
.env.production.local

# Configuration files with secrets
appsettings.Production.json
appsettings.Development.json
secrets.json

# SSL/TLS certificates (local testing)
ssl/
*.pem
*.key
*.crt

# Application logs
logs/
*.log
*.log.*

# Temporary files
.tmp/
temp/

# IDE
.vscode/
.idea/
*.swp
*.swo
*~

# OS
.DS_Store
Thumbs.db
```

### Шаг 3: Обновите `docker-compose.yml`

```yaml
version: '3.8'

services:
  mysql:
    image: mysql:5.7
    container_name: zettechavio-mysql
    environment:
      MYSQL_ROOT_PASSWORD: ${DB_PASSWORD:-default_password_change_me}
      MYSQL_DATABASE: ${MYSQL_DATABASE:-ZetTechAvioDB}
    # ... остальное без изменений

  backend:
    build:
      context: ./project/ZetTechAvio1.0
      dockerfile: Dockerfile
    container_name: zettechavio-backend
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}
      ConnectionStrings__DefaultConnection: "Server=mysql;Database=${MYSQL_DATABASE};User=root;Password=${DB_PASSWORD};SslMode=none;"
      HttpClient__BaseAddress: ${HttpClient__BaseAddress}
      Cors__AllowedOrigins: ${Cors__AllowedOrigins}
      Urls: ${Urls}
      JWT_SECRET: ${JWT_SECRET}
      YOOKASSA_SHOP_ID: ${YOOKASSA_SHOP_ID}
      YOOKASSA_API_KEY: ${YOOKASSA_API_KEY}
      SMTP_PASSWORD: ${SMTP_PASSWORD}
    depends_on:
      mysql:
        condition: service_healthy
    ports:
      - "5151:5151"
    networks:
      - zettechavio-network
    restart: unless-stopped
```

### Шаг 4: Генерируйте JWT_SECRET

```bash
# Generate random JWT secret (256-bit = 32 bytes = 64 hex chars)
openssl rand -hex 32

# Output example: a3f5b8c9d7e2f1a4b6c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9
```

---

## 📋 Чек-лист перед публикацией в PUBLIC

### 🔴 КРИТИЧЕСКОЕ (без этого НЕ публиковать):
- [x] `.env` файл удален из git истории
- [x] `.env` добавлен в `.gitignore`
- [x] `.env.example` создан и закоммичен
- [ ] Все пароли смены в реальных сервисах (MySQL, YooKassa, Mail.ru)
- [ ] HTTPS включен и работает (use Let's Encrypt)
- [ ] CORS ограничен вашим доменом (не *)
- [x] JWT сгенерирован (не дефолтный)

### 🟡 ВАЖНОЕ (сильно рекомендуется):
- [ ] README.md содержит SECURITY раздел
- [x] Инструкция по копированию `.env.example` → `.env`
- [x] GitHub репо имеет SECURITY.md
- [ ] Branch protection включена (Require PR review)
- [ ] Dependabot alerts включены
- [ ] GitHub Secret Scanning включено

### 🟢 ЖЕЛАТЕЛЬНОЕ (для production):
- [ ] GitHub Advanced Security (Code scanning)
- [ ] SAST инструменты (SonarQube, Snyk)
- [ ] Docker image signing
- [ ] Semantic versioning tags
- [ ] Changelog поддерживается

### ✅ CI/CD Status:
- [x] GitHub Actions workflow создан и работает
- [x] Docker образы собираются в ghcr.io
- [x] SSH деплой на VPS работает
- [x] docker compose pull и up выполняются успешно
- [ ] Файлы реально обновляются на VPS (нужна проверка WinSCP)

---

## 🔧 FIREWALL проверка на VPS

### 1. Проверьте какие порты открыты:

```bash
# Посмотреть что слушает
sudo ss -tlnp

# Ожидаемый результат:
# LISTEN 0.0.0.0:80         nginx
# LISTEN 0.0.0.0:443        nginx
# LISTEN 0.0.0.0:3306       mysql (внутри docker)
# LISTEN 0.0.0.0:5151       backend (внутри docker)
```

### 2. Проверьте UFW (Linux firewall):

```bash
sudo ufw status

# Ожидаемый: Status: inactive (если LandVPS управляет на уровне провайдера)

# Если надо открыть:
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 22/tcp  # SSH
sudo ufw enable
```

### 3. Проверьте в панели LandVPS:

```
LandVPS Dashboard → Security/Firewall → Входящие правила:
  ✅ 80 TCP (HTTP)
  ✅ 443 TCP (HTTPS)
  ✅ 22 TCP (SSH)
  ❌ Заблокированы остальные
```

### 4. Протестируйте с локальной машины:

```bash
# Проверить доступность portов
curl -v https://193.163.170.82/
curl -v http://193.163.170.82/

# Проверить DNS
nslookup zettechavio.ru
dig zettechavio.ru

# Пинг
ping 193.163.170.82
```

---

## 📊 Генерация нормальных паролей

```bash
# Метод 1: OpenSSL
openssl rand -base64 32

# Метод 2: Python
python3 -c "import secrets; print(secrets.token_urlsafe(32))"

# Метод 3: /dev/urandom
head -c 32 /dev/urandom | base64

# Результат должен быть: минимум 16+ символов, буквы + цифры + спецсимволы
```

---

## 🔐 Как структурировать репо для PUBLIC

### Два подхода:

#### ✅ Вариант 1: Старый (приватный) + Новый (публичный)

```
ZetTechAvio (PRIVATE - на локальной машине)
  ├─ .git/  (вся история с паролями)
  ├─ .env   (реальные пароли)
  └─ (для вашей справки и разработки)

ZetTechAvio-public (PUBLIC - на GitHub)
  ├─ .git/  (чистая история БЕЗ паролей)
  ├─ .env.example (только шаблон)
  ├─ SECURITY.md
  ├─ README.md (с инструкциями)
  └─ (видна всем)
```

#### ⚠️ Что НИКОГДА не публиковать:

```
❌ настоящие .env файлы
❌ appsettings.Production.json с паролями
❌ komodo, api keys, JWT секреты
❌ private key сертификатов
❌ логи с чувствительной информацией
❌ node_modules/, bin/, obj/
```

---

## 📝 Как перейти на PUBLIC правильно

### Команды для миграции:

```bash
# На локальной машине (Windows)

# 1. Проверьте что expose в истории
git log --all -S "12344" --oneline
git log --all -S "password" --oneline

# 2. Если много утечек - переписать историю (dangerous!)
# ТОЛЬКО делайте если никто не использует старый репо!
git filter-repo --replace-text <(echo "12344%&J==>REDACTED")

# 3. Создайте чистый репо для public
git clone --depth 1 . ../ZetTechAvio-Public
cd ../ZetTechAvio-Public
rm -rf .git
git init
git add .
git commit -m "ZetTechAvio - Public Release v1.0"
git remote add origin https://github.com/YOUR_USERNAME/ZetTechAvio.git
git push -u origin main

# 4. Старый репо оставьте как приватный backup
cd ../ZetTechAvio
git remote rename origin backup
git log --oneline | head -20  # смотрите историю локально
```

---

## 🚀 Команды для VPS после security фиксов

```bash
cd /root/ZetTechAvio

# 1. Создайте .env с новыми паролями
cat > .env << 'EOF'
DB_PASSWORD=NewSecurePass!@#2026
MYSQL_DATABASE=ZetTechAvioDB
ASPNETCORE_ENVIRONMENT=Production
HttpClient__BaseAddress=https://api.zettechavio.ru
Cors__AllowedOrigins=https://zettechavio.ru;https://www.zettechavio.ru
JWT_SECRET=$(openssl rand -hex 32)
YOOKASSA_SHOP_ID=YOUR_SHOP_ID
YOOKASSA_API_KEY=YOUR_API_KEY
SMTP_PASSWORD=YOUR_SMTP_PASSWORD
EOF

# 2. Запустите с .env
docker-compose --env-file .env down
docker-compose --env-file .env up -d

# 3. Проверьте
docker-compose ps
curl -k https://localhost/
```

---

## 📞 Контакты для Security Issues

Если кто-то найдет уязвимость:

**SECURITY.md в репо:**
```markdown
# Security Policy

## Reporting Security Vulnerabilities

Please email security@zettechavio.ru with:
- Description of the vulnerability
- Steps to reproduce
- Potential impact

Do NOT open public issues for security vulnerabilities!
```

---

## ✨ Итого что сделать ПО ПОРЯДКУ:

1. **Сейчас:**
   - [ ] Поменяйте MySQL пароль на VPS
   - [ ] Скачайте все API ключи (YooKassa, Mail.ru)
   - [ ] Переставьте их в сервисах

2. **В течение часа:**
   - [ ] Создайте `.env.example` в проекте
   - [ ] Обновите `.gitignore`
   - [ ] Убедитесь `.env` не коммичен

3. **Перед публикацией:**
   - [ ] Проверьте проверили git历史 (`git log --all -S "password"`)
   - [ ] Создайте чистый репо для PUBLIC
   - [ ] Добавьте SECURITY.md
   - [ ] Протестируйте docker-compose с `.env`

4. **После публикации:**
   - [ ] Включите GitHub Branch Protection
   - [ ] Включите Dependabot alerts
   - [ ] Мониторьте GitHub Security alerts

---

✅ **Всё готово к публикации после этих шагов!**
