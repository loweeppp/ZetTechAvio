CREATE DATABASE  IF NOT EXISTS `zettechaviodb` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `zettechaviodb`;
-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: localhost    Database: zettechaviodb
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20251118093212_InitialCreate','9.0.0'),('20251125142642_AddAviationTables','9.0.0'),('20251201144448_MakeLogoNullable','9.0.0'),('20251208194845_AddFares','9.0.0'),('20251208200730_FixPriceDecimalPrecision','9.0.0'),('20260130160630_FaresInFlights','9.0.0'),('20260130161424_FaresInFlights2','9.0.0'),('20260227163318_AddBookingsSeatsTicketsV2','9.0.0'),('20260227170926_AddPassportToTickets','9.0.0'),('20260312160422_AddConfirmationCodes','9.0.0'),('20260403064737_AddPaymentTable','9.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aircraft`
--

DROP TABLE IF EXISTS `aircraft`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aircraft` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Model` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Manufacturer` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `TotalSeats` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `idx_model` (`Model`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aircraft`
--

LOCK TABLES `aircraft` WRITE;
/*!40000 ALTER TABLE `aircraft` DISABLE KEYS */;
INSERT INTO `aircraft` VALUES (1,'Boeing 737','Boeing',189,'2025-12-18 22:03:16.000000'),(2,'Airbus A320','Airbus',194,'2025-12-18 22:03:16.000000'),(3,'Boeing 777','Boeing',350,'2025-12-18 22:03:16.000000'),(4,'Airbus A330','Airbus',335,'2025-12-18 22:03:16.000000'),(5,'Embraer E190','Embraer',124,'2025-12-18 22:03:16.000000');
/*!40000 ALTER TABLE `aircraft` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `airlines`
--

DROP TABLE IF EXISTS `airlines`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `airlines` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `IataCode` varchar(3) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LogoUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `idx_iata` (`IataCode`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `airlines`
--

LOCK TABLES `airlines` WRITE;
/*!40000 ALTER TABLE `airlines` DISABLE KEYS */;
INSERT INTO `airlines` VALUES (1,'SU','Aeroflot',NULL,'2025-12-18 22:03:10.000000'),(2,'UT','Utair',NULL,'2025-12-18 22:03:10.000000'),(3,'S7','S7 Airlines',NULL,'2025-12-18 22:03:10.000000');
/*!40000 ALTER TABLE `airlines` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `airports`
--

DROP TABLE IF EXISTS `airports`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `airports` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Iata` varchar(3) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `City` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Country` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Lat` double NOT NULL,
  `Lon` double NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `idx_city` (`City`),
  KEY `idx_country` (`Country`),
  KEY `idx_iata` (`Iata`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `airports`
--

LOCK TABLES `airports` WRITE;
/*!40000 ALTER TABLE `airports` DISABLE KEYS */;
INSERT INTO `airports` VALUES (1,'DME','Домодедово Международный Аэропорт','Москва','Россия',55.4084,37.9015,'2025-12-18 22:03:16.000000'),(2,'LED','Пулково Аэропорт ','Санкт-Петербург','Россия',59.8011,30.2625,'2025-12-18 22:03:16.000000'),(3,'KZN','Казань Международный Аэропорт ','Казань','Россия',55.6064,49.2865,'2025-12-18 22:03:16.000000'),(4,'KRR','Анапа Аэропорт','Сочи','Россия',43.4421,39.947,'2025-12-18 22:03:16.000000');
/*!40000 ALTER TABLE `airports` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bookings`
--

DROP TABLE IF EXISTS `bookings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bookings` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `BookingReference` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `TotalAmount` decimal(10,2) NOT NULL,
  `Status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PaymentMethod` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `idx_booking_ref` (`BookingReference`),
  KEY `idx_created` (`CreatedAt`),
  KEY `idx_status` (`Status`),
  KEY `idx_user` (`UserId`),
  CONSTRAINT `FK_Bookings_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bookings`
--

LOCK TABLES `bookings` WRITE;
/*!40000 ALTER TABLE `bookings` DISABLE KEYS */;
/*!40000 ALTER TABLE `bookings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `fares`
--

DROP TABLE IF EXISTS `fares`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `fares` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FlightId` int NOT NULL,
  `Currency` varchar(3) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Price` decimal(12,2) NOT NULL,
  `SeatsAvailable` int NOT NULL,
  `BaggageIncluded` tinyint(1) NOT NULL,
  `BaggageWeightKg` int NOT NULL,
  `Refundable` tinyint(1) NOT NULL,
  `Class` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `idx_class` (`Class`),
  KEY `idx_flight_id` (`FlightId`),
  CONSTRAINT `FK_Fares_Flights_FlightId` FOREIGN KEY (`FlightId`) REFERENCES `flights` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `fares`
--

LOCK TABLES `fares` WRITE;
/*!40000 ALTER TABLE `fares` DISABLE KEYS */;
INSERT INTO `fares` VALUES (1,5,'RUB',4299.00,50,1,23,0,'Economy'),(2,5,'RUB',8500.00,30,1,23,1,'Business'),(3,5,'RUB',15000.00,10,1,32,1,'First'),(4,6,'RUB',3850.00,60,0,0,0,'Economy'),(5,6,'RUB',7200.00,25,1,23,1,'Business'),(6,7,'RUB',5500.00,45,1,23,0,'Economy'),(7,7,'RUB',10200.00,20,1,23,1,'Business'),(8,7,'RUB',18000.00,8,1,32,1,'First'),(9,8,'RUB',4500.00,55,1,23,0,'Economy'),(10,8,'RUB',9000.00,28,1,23,1,'Business');
/*!40000 ALTER TABLE `fares` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `flights`
--

DROP TABLE IF EXISTS `flights`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `flights` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FlightNumber` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `AirlineId` int NOT NULL,
  `AircraftId` int NOT NULL,
  `OriginAirportId` int NOT NULL,
  `DestAirportId` int NOT NULL,
  `DepartureDt` datetime(6) NOT NULL,
  `ArrivalDt` datetime(6) NOT NULL,
  `DurationMinutes` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `Status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `idx_airline` (`AirlineId`),
  KEY `idx_departure` (`DepartureDt`),
  KEY `idx_flight_number` (`FlightNumber`),
  KEY `idx_route` (`OriginAirportId`,`DestAirportId`),
  KEY `idx_status` (`Status`),
  KEY `IX_Flights_AircraftId` (`AircraftId`),
  KEY `IX_Flights_DestAirportId` (`DestAirportId`),
  CONSTRAINT `FK_Flights_Aircraft_AircraftId` FOREIGN KEY (`AircraftId`) REFERENCES `aircraft` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Flights_Airlines_AirlineId` FOREIGN KEY (`AirlineId`) REFERENCES `airlines` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Flights_Airports_DestAirportId` FOREIGN KEY (`DestAirportId`) REFERENCES `airports` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Flights_Airports_OriginAirportId` FOREIGN KEY (`OriginAirportId`) REFERENCES `airports` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `flights`
--

LOCK TABLES `flights` WRITE;
/*!40000 ALTER TABLE `flights` DISABLE KEYS */;
INSERT INTO `flights` VALUES (5,'SU112',1,1,1,2,'2025-12-18 22:21:03.000000','2025-12-19 01:21:03.000000',185,'2025-12-18 22:21:03.000000','Scheduled'),(6,'UT328',2,2,1,3,'2025-12-18 22:21:03.000000','2025-12-19 00:21:03.000000',126,'2025-12-18 22:21:03.000000','Scheduled'),(7,'UT128',2,3,1,4,'2025-12-18 22:21:03.000000','2025-12-19 03:21:03.000000',307,'2025-12-18 22:21:03.000000','Scheduled'),(8,'S7115',3,1,1,2,'2025-12-18 22:21:03.000000','2025-12-19 01:21:03.000000',179,'2025-12-18 22:21:03.000000','Scheduled');
/*!40000 ALTER TABLE `flights` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `payments`
--

DROP TABLE IF EXISTS `payments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `payments` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `BookingId` int NOT NULL,
  `YooKassaPaymentId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `TotalAmount` decimal(10,2) NOT NULL,
  `Status` int NOT NULL,
  `ConfirmationUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PaymentMethod` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Discription` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `idx_yookassa_payment_id` (`YooKassaPaymentId`),
  KEY `idx_booking_id` (`BookingId`),
  CONSTRAINT `FK_Payments_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `bookings` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `payments`
--

LOCK TABLES `payments` WRITE;
/*!40000 ALTER TABLE `payments` DISABLE KEYS */;
/*!40000 ALTER TABLE `payments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `seats`
--

DROP TABLE IF EXISTS `seats`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `seats` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FlightId` int NOT NULL,
  `SeatNumber` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SeatClass` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `unique_seat_per_flight` (`FlightId`,`SeatNumber`),
  KEY `idx_class` (`SeatClass`),
  KEY `idx_flight` (`FlightId`),
  KEY `idx_status` (`Status`),
  CONSTRAINT `FK_Seats_Flights_FlightId` FOREIGN KEY (`FlightId`) REFERENCES `flights` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `seats`
--

LOCK TABLES `seats` WRITE;
/*!40000 ALTER TABLE `seats` DISABLE KEYS */;
/*!40000 ALTER TABLE `seats` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tickets`
--

DROP TABLE IF EXISTS `tickets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tickets` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `BookingId` int NOT NULL,
  `FlightId` int NOT NULL,
  `FareId` int NOT NULL,
  `SeatId` int DEFAULT NULL,
  `TicketNumber` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PassengerName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PassengerType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PassengerDob` datetime(6) DEFAULT NULL,
  `Price` decimal(10,2) NOT NULL,
  `Status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  `PassportNumber` varchar(6) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '',
  `PassportSeries` varchar(4) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `idx_ticket_number` (`TicketNumber`),
  KEY `idx_booking` (`BookingId`),
  KEY `idx_flight` (`FlightId`),
  KEY `idx_status` (`Status`),
  KEY `IX_Tickets_FareId` (`FareId`),
  KEY `IX_Tickets_SeatId` (`SeatId`),
  CONSTRAINT `FK_Tickets_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `bookings` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Tickets_Fares_FareId` FOREIGN KEY (`FareId`) REFERENCES `fares` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Tickets_Flights_FlightId` FOREIGN KEY (`FlightId`) REFERENCES `flights` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Tickets_Seats_SeatId` FOREIGN KEY (`SeatId`) REFERENCES `seats` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tickets`
--

LOCK TABLES `tickets` WRITE;
/*!40000 ALTER TABLE `tickets` DISABLE KEYS */;
/*!40000 ALTER TABLE `tickets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PasswordHash` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FullName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Phone` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Role` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `idx_email` (`Email`),
  KEY `idx_role` (`Role`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (2,'pop@gmail.com','$2a$11$Q045xlabPb0WSzMN8XXXYOaNJMGIS2./6mZrFApGL7kRm67g9GhXC','pop','9898','User','2026-01-17 06:16:56.797835','2026-01-17 06:16:56.797836'),(3,'23rolling@gmail.com','$2a$11$o/HpCFeGlvHfAoHe.fLT/uMmee/BxUQ4TWyc4xiAFSH6srWJudpuS','rolliingUser','+78909088765','User','2026-01-30 09:17:23.340705','2026-01-30 09:17:23.340705'),(4,'kolller@gmail.com','$2a$11$XGdS3FwWFL5sO6EauRvonO84oY0bFFIiAAZs3ZXIMqLOubfg1sk4S','koll3','+79054503423','User','2026-03-11 15:12:18.913989','2026-03-11 15:12:18.914030'),(5,'fdfdsfs@gmail.com','$2a$11$ybzUWJXkOH5t6bf2176Ub./n5EJb76U/gDdZAGXjnJvYY5WVWXm3e','dgsg','+7898765334','User','2026-03-13 14:10:24.889230','2026-03-13 14:10:24.889260'),(6,'565dfdfd@gmail.com','$2a$11$.I27m7yfTvW2VdkwCe5GI.QkPxQ0pIKWFAi0MePSjnXd1t6Tlt8gC','fdfd','789876545678','User','2026-03-13 15:42:23.322197','2026-03-13 15:42:23.322218'),(7,'kjhgfd@gamil.com','$2a$11$O90F/t2nkGBJgPs/wt.G.uZpM1FNs4ZXG9DWn2Fn8OjYOpcdKtfv6','juhgfd','787654345678','User','2026-03-14 07:32:52.446331','2026-03-14 07:32:52.446332'),(8,'lowep980@gmail.com','$2a$11$GRSDNSa33NJ7V9TifdafWe.ksOOZGshfEoJkxZfprs2IMee9qfL1C','Lowep','79606953553','User','2026-03-17 07:02:25.009251','2026-03-17 07:02:25.009288');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-03 18:53:26
