-- Восстановление данных ZetTechAvio

USE `ZetTechAvioDB`;

-- Airlines
INSERT INTO `Airlines` (`IataCode`, `Name`, `LogoUrl`, `CreatedAt`) VALUES
('SU', 'Aeroflot', NULL, NOW()),
('UT', 'Utair', NULL, NOW()),
('S7', 'S7 Airlines', NULL, NOW());

-- Airports
INSERT INTO `Airports` (`Iata`, `Name`, `City`, `Country`, `Lat`, `Lon`, `CreatedAt`) VALUES
('DME', 'Domodedovo International Airport', 'Moscow', 'Russia', 55.4084, 37.9015, NOW()),
('LED', 'Pulkovo Airport', 'Saint Petersburg', 'Russia', 59.8011, 30.2625, NOW()),
('KZN', 'Kazan International Airport', 'Kazan', 'Russia', 55.6064, 49.2865, NOW()),
('KRR', 'Anapa Airport', 'Sochi', 'Russia', 43.4421, 39.9470, NOW());

-- Aircraft
INSERT INTO `Aircraft` (`Model`, `Manufacturer`, `TotalSeats`, `CreatedAt`) VALUES
('Boeing 737', 'Boeing', 189, NOW()),
('Airbus A320', 'Airbus', 194, NOW()),
('Boeing 777', 'Boeing', 350, NOW()),
('Airbus A330', 'Airbus', 335, NOW()),
('Embraer E190', 'Embraer', 124, NOW());

-- Flights
INSERT INTO `Flights` (`FlightNumber`, `AirlineId`, `AircraftId`, `OriginAirportId`, `DestAirportId`, `DepartureDt`, `ArrivalDt`, `DurationMinutes`, `Status`, `CreatedAt`) VALUES
('SU112', 1, 1, 1, 2, NOW(), DATE_ADD(NOW(), INTERVAL 3 HOUR), 180, 'Scheduled', NOW()),
('UT328', 2, 2, 1, 3, NOW(), DATE_ADD(NOW(), INTERVAL 2 HOUR), 120, 'Scheduled', NOW()),
('UT128', 2, 3, 1, 4, NOW(), DATE_ADD(NOW(), INTERVAL 5 HOUR), 300, 'Scheduled', NOW()),
('S7115', 3, 1, 1, 2, NOW(), DATE_ADD(NOW(), INTERVAL 3 HOUR), 180, 'Scheduled', NOW());

-- Fares (восстановленные из binlog)
INSERT INTO `Fares` (`Id`, `FlightId`, `Currency`, `Price`, `SeatsAvailable`, `BaggageIncluded`, `BaggageWeightKg`, `Refundable`, `Class`) VALUES
(1, 1, 'RUB', 4299.00, 50, 1, 23, 0, 'Economy'),
(2, 1, 'RUB', 8500.00, 30, 1, 23, 1, 'Business'),
(3, 1, 'RUB', 15000.00, 10, 1, 32, 1, 'First'),
(4, 2, 'RUB', 3850.00, 60, 0, 0, 0, 'Economy'),
(5, 2, 'RUB', 7200.00, 25, 1, 23, 1, 'Business'),
(6, 3, 'RUB', 5500.00, 45, 1, 23, 0, 'Economy'),
(7, 3, 'RUB', 10200.00, 20, 1, 23, 1, 'Business'),
(8, 3, 'RUB', 18000.00, 8, 1, 32, 1, 'First'),
(9, 4, 'RUB', 4500.00, 55, 1, 23, 0, 'Economy'),
(10, 4, 'RUB', 9000.00, 28, 1, 23, 1, 'Business');
