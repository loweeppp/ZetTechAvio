

-- Создаём базу данных
CREATE DATABASE ZetTechAvioDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE ZetTechAvioDB;

-- ============================================
-- 1. Справочник авиакомпаний
-- ============================================
CREATE TABLE Airlines (
    id INT PRIMARY KEY AUTO_INCREMENT,
    iata_code VARCHAR(3) UNIQUE NOT NULL COMMENT 'Код IATA (SU, S7)',
    name VARCHAR(255) NOT NULL COMMENT 'Название авиакомпании',
    logo_url VARCHAR(500) COMMENT 'URL логотипа',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_iata (iata_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Справочник авиакомпаний';

-- ============================================
-- 2. Справочник аэропортов
-- ============================================
CREATE TABLE Airports (
    id INT PRIMARY KEY AUTO_INCREMENT,
    iata VARCHAR(3) UNIQUE NOT NULL COMMENT 'Код IATA (DME, LED)',
    name VARCHAR(255) NOT NULL COMMENT 'Название аэропорта',
    city VARCHAR(255) NOT NULL COMMENT 'Город',
    country VARCHAR(100) NOT NULL COMMENT 'Страна',
    lat DOUBLE COMMENT 'Широта',
    lon DOUBLE COMMENT 'Долгота',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_iata (iata),
    INDEX idx_city (city),
    INDEX idx_country (country)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Справочник аэропортов';

-- ============================================
-- 3. Типы самолётов
-- ============================================
CREATE TABLE Aircraft (
    id INT PRIMARY KEY AUTO_INCREMENT,
    model VARCHAR(100) NOT NULL COMMENT 'Модель (Boeing 737-800)',
    manufacturer VARCHAR(100) NOT NULL COMMENT 'Производитель (Boeing)',
    total_seats INT NOT NULL COMMENT 'Общее количество мест',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_model (model)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Типы самолётов';

-- ============================================
-- 4. Схемы салона (схема мест для типа самолёта)
-- ============================================
CREATE TABLE SeatMaps (
    id INT PRIMARY KEY AUTO_INCREMENT,
    aircraft_id INT NOT NULL COMMENT 'Тип самолёта',
    seat_number VARCHAR(10) NOT NULL COMMENT 'Номер места (12A, 15C)',
    seat_class ENUM('economy', 'business', 'first') NOT NULL COMMENT 'Класс места',
    row_number INT NOT NULL COMMENT 'Номер ряда',
    seat_letter VARCHAR(2) NOT NULL COMMENT 'Буква места (A, B, C)',
    is_window BOOLEAN DEFAULT FALSE COMMENT 'У окна',
    is_aisle BOOLEAN DEFAULT FALSE COMMENT 'У прохода',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (aircraft_id) REFERENCES Aircraft(id) ON DELETE CASCADE,
    UNIQUE KEY unique_seat_per_aircraft (aircraft_id, seat_number),
    INDEX idx_aircraft (aircraft_id),
    INDEX idx_class (seat_class)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Схемы салона самолётов';

-- ============================================
-- 5. Рейсы
-- ============================================
CREATE TABLE Flights (
    id INT PRIMARY KEY AUTO_INCREMENT,
    flight_number VARCHAR(50) NOT NULL COMMENT 'Номер рейса (SU 112)',
    airline_id INT NOT NULL COMMENT 'Авиакомпания',
    aircraft_id INT NOT NULL COMMENT 'Тип самолёта',
    origin_airport_id INT NOT NULL COMMENT 'Аэропорт вылета',
    dest_airport_id INT NOT NULL COMMENT 'Аэропорт прилёта',
    departure_dt DATETIME NOT NULL COMMENT 'Дата и время вылета',
    arrival_dt DATETIME NOT NULL COMMENT 'Дата и время прилёта',
    duration_minutes INT NOT NULL COMMENT 'Длительность в минутах',
    status ENUM('scheduled', 'delayed', 'cancelled', 'completed') DEFAULT 'scheduled' COMMENT 'Статус рейса',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (airline_id) REFERENCES Airlines(id) ON DELETE RESTRICT,
    FOREIGN KEY (aircraft_id) REFERENCES Aircraft(id) ON DELETE RESTRICT,
    FOREIGN KEY (origin_airport_id) REFERENCES Airports(id) ON DELETE RESTRICT,
    FOREIGN KEY (dest_airport_id) REFERENCES Airports(id) ON DELETE RESTRICT,
    INDEX idx_flight_number (flight_number),
    INDEX idx_departure (departure_dt),
    INDEX idx_route (origin_airport_id, dest_airport_id),
    INDEX idx_airline (airline_id),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Рейсы';

-- ============================================
-- 6. Тарифы (цены и условия по классам)
-- ============================================
CREATE TABLE Fares (
    id INT PRIMARY KEY AUTO_INCREMENT,
    flight_id INT NOT NULL COMMENT 'Рейс',
    fare_class ENUM('economy', 'business', 'first') NOT NULL COMMENT 'Класс тарифа',
    price DECIMAL(10, 2) NOT NULL COMMENT 'Цена билета',
    baggage_included BOOLEAN DEFAULT FALSE COMMENT 'Багаж включён',
    baggage_weight INT DEFAULT 0 COMMENT 'Вес багажа (кг)',
    refundable BOOLEAN DEFAULT FALSE COMMENT 'Возвратный',
    seats_available INT NOT NULL COMMENT 'Доступно мест',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (flight_id) REFERENCES Flights(id) ON DELETE CASCADE,
    UNIQUE KEY unique_fare_per_flight (flight_id, fare_class),
    INDEX idx_flight (flight_id),
    INDEX idx_class (fare_class),
    INDEX idx_price (price)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Тарифы на рейсы';

-- ============================================
-- 7. Места в конкретном рейсе
-- ============================================
CREATE TABLE Seats (
    id INT PRIMARY KEY AUTO_INCREMENT,
    flight_id INT NOT NULL COMMENT 'Рейс',
    seat_number VARCHAR(10) NOT NULL COMMENT 'Номер места (12A)',
    seat_class ENUM('economy', 'business', 'first') NOT NULL COMMENT 'Класс места',
    status ENUM('available', 'reserved', 'booked') DEFAULT 'available' COMMENT 'Статус места',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (flight_id) REFERENCES Flights(id) ON DELETE CASCADE,
    UNIQUE KEY unique_seat_per_flight (flight_id, seat_number),
    INDEX idx_flight (flight_id),
    INDEX idx_status (status),
    INDEX idx_class (seat_class)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Места в рейсах';

-- ============================================
-- 8. Пользователи
-- ============================================
CREATE TABLE Users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    email VARCHAR(255) UNIQUE NOT NULL COMMENT 'Email (логин)',
    password_hash VARCHAR(255) NOT NULL COMMENT 'Хэш пароля',
    full_name VARCHAR(255) NOT NULL COMMENT 'ФИО',
    phone VARCHAR(20) COMMENT 'Телефон',
    role ENUM('admin', 'manager', 'user') DEFAULT 'user' COMMENT 'Роль пользователя',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_email (email),
    INDEX idx_role (role)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Пользователи системы';

-- ============================================
-- 9. Заказы (один заказ может включать несколько билетов)
-- ============================================
CREATE TABLE Bookings (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL COMMENT 'Пользователь',
    booking_reference VARCHAR(10) UNIQUE NOT NULL COMMENT 'Номер заказа (PNR)',
    total_amount DECIMAL(10, 2) NOT NULL COMMENT 'Общая сумма',
    status ENUM('created', 'paid', 'confirmed', 'cancelled') DEFAULT 'created' COMMENT 'Статус заказа',
    payment_method VARCHAR(50) COMMENT 'Метод оплаты',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE RESTRICT,
    INDEX idx_user (user_id),
    INDEX idx_booking_ref (booking_reference),
    INDEX idx_status (status),
    INDEX idx_created (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Заказы билетов';

-- ============================================
-- 10. Билеты (один билет = один пассажир на один рейс)
-- ============================================
CREATE TABLE Tickets (
    id INT PRIMARY KEY AUTO_INCREMENT,
    booking_id INT NOT NULL COMMENT 'Заказ',
    flight_id INT NOT NULL COMMENT 'Рейс',
    fare_id INT NOT NULL COMMENT 'Тариф',
    seat_id INT COMMENT 'Место (может быть NULL если не выбрано)',
    ticket_number VARCHAR(20) UNIQUE NOT NULL COMMENT 'Номер билета',
    passenger_name VARCHAR(255) NOT NULL COMMENT 'ФИО пассажира',
    passenger_type ENUM('adult', 'child', 'infant') DEFAULT 'adult' COMMENT 'Тип пассажира',
    passenger_dob DATE COMMENT 'Дата рождения пассажира',
    price DECIMAL(10, 2) NOT NULL COMMENT 'Цена билета',
    status ENUM('active', 'cancelled', 'used') DEFAULT 'active' COMMENT 'Статус билета',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (booking_id) REFERENCES Bookings(id) ON DELETE CASCADE,
    FOREIGN KEY (flight_id) REFERENCES Flights(id) ON DELETE RESTRICT,
    FOREIGN KEY (fare_id) REFERENCES Fares(id) ON DELETE RESTRICT,
    FOREIGN KEY (seat_id) REFERENCES Seats(id) ON DELETE SET NULL,
    INDEX idx_booking (booking_id),
    INDEX idx_flight (flight_id),
    INDEX idx_ticket_number (ticket_number),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Билеты';

-- ============================================
-- Создание триггеров
-- ============================================

-- Триггер: при создании билета обновляем статус места на 'booked'
DELIMITER //
CREATE TRIGGER trg_ticket_seat_book
AFTER INSERT ON Tickets
FOR EACH ROW
BEGIN
    IF NEW.seat_id IS NOT NULL THEN
        UPDATE Seats 
        SET status = 'booked' 
        WHERE id = NEW.seat_id;
    END IF;
END//
DELIMITER ;

-- Триггер: при отмене билета освобождаем место
DELIMITER //
CREATE TRIGGER trg_ticket_seat_cancel
AFTER UPDATE ON Tickets
FOR EACH ROW
BEGIN
    IF NEW.status = 'cancelled' AND OLD.status != 'cancelled' AND NEW.seat_id IS NOT NULL THEN
        UPDATE Seats 
        SET status = 'available' 
        WHERE id = NEW.seat_id;
    END IF;
END//
DELIMITER ;

-- Триггер: при создании билета уменьшаем количество доступных мест в тарифе
DELIMITER //
CREATE TRIGGER trg_ticket_decrease_fare_seats
AFTER INSERT ON Tickets
FOR EACH ROW
BEGIN
    UPDATE Fares 
    SET seats_available = seats_available - 1 
    WHERE id = NEW.fare_id AND seats_available > 0;
END//
DELIMITER ;

-- ============================================
-- Создание представлений (Views)
-- ============================================

-- Представление: Детальная информация о рейсах
CREATE VIEW vw_FlightDetails AS
SELECT 
    f.id,
    f.flight_number,
    al.name AS airline_name,
    al.iata_code AS airline_code,
    ac.model AS aircraft_model,
    orig.iata AS origin_code,
    orig.city AS origin_city,
    dest.iata AS dest_code,
    dest.city AS dest_city,
    f.departure_dt,
    f.arrival_dt,
    f.duration_minutes,
    f.status,
    (SELECT MIN(price) FROM Fares WHERE flight_id = f.id) AS min_price,
    (SELECT SUM(seats_available) FROM Fares WHERE flight_id = f.id) AS total_available_seats
FROM Flights f
INNER JOIN Airlines al ON f.airline_id = al.id
INNER JOIN Aircraft ac ON f.aircraft_id = ac.id
INNER JOIN Airports orig ON f.origin_airport_id = orig.id
INNER JOIN Airports dest ON f.dest_airport_id = dest.id;

-- Представление: История заказов пользователя
CREATE VIEW vw_UserBookings AS
SELECT 
    b.id AS booking_id,
    b.booking_reference,
    b.total_amount,
    b.status AS booking_status,
    b.created_at AS booking_date,
    u.full_name AS user_name,
    u.email AS user_email,
    COUNT(t.id) AS total_tickets
FROM Bookings b
INNER JOIN Users u ON b.user_id = u.id
LEFT JOIN Tickets t ON b.id = t.booking_id
GROUP BY b.id, b.booking_reference, b.total_amount, b.status, b.created_at, u.full_name, u.email;

-- ============================================
-- Создание функций
-- ============================================

-- Функция: Генерация номера заказа (PNR)
DELIMITER //
CREATE FUNCTION fn_GenerateBookingReference()
RETURNS VARCHAR(10)
DETERMINISTIC
BEGIN
    DECLARE ref VARCHAR(10);
    DECLARE chars VARCHAR(36) DEFAULT 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    DECLARE i INT DEFAULT 0;
    SET ref = '';
    WHILE i < 6 DO
        SET ref = CONCAT(ref, SUBSTRING(chars, FLOOR(1 + RAND() * 34), 1));
        SET i = i + 1;
    END WHILE;
    RETURN ref;
END//
DELIMITER ;

-- Функция: Генерация номера билета
DELIMITER //
CREATE FUNCTION fn_GenerateTicketNumber(booking_ref VARCHAR(10))
RETURNS VARCHAR(20)
DETERMINISTIC
BEGIN
    DECLARE seq INT;
    SELECT COUNT(*) + 1 INTO seq FROM Tickets WHERE booking_id = (SELECT id FROM Bookings WHERE booking_reference = booking_ref);
    RETURN CONCAT(booking_ref, '-', LPAD(seq, 3, '0'));
END//
DELIMITER ;

