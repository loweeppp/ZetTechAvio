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
INSERT INTO zettechaviodb.fares VALUES
(11, 9, 'RUB', 4299.00, 44, 1, 23, 0, 'Economy'),
(12, 9, 'RUB', 8500.00, 30, 1, 23, 1, 'Business'),
(13, 10, 'RUB', 4299.00, 44, 1, 23, 0, 'Economy'),
(14, 10, 'RUB', 8500.00, 30, 1, 23, 1, 'Business'),
(15, 11, 'RUB', 3850.00, 56, 0, 0, 0, 'Economy'),
(16, 11, 'RUB', 7200.00, 23, 1, 23, 1, 'Business'),
(17, 12, 'RUB', 4500.00, 54, 1, 23, 0, 'Economy'),
(18, 12, 'RUB', 9000.00, 28, 1, 23, 1, 'Business'),
(19, 13, 'RUB', 5500.00, 40, 1, 23, 0, 'Economy'),
(20, 13, 'RUB', 10200.00, 20, 1, 23, 1, 'Business'),
(21, 14, 'RUB', 4500.00, 54, 1, 23, 0, 'Economy'),
(22, 14, 'RUB', 9000.00, 28, 1, 23, 1, 'Business'),
(23, 15, 'RUB', 5600.00, 42, 1, 23, 0, 'Economy'),
(24, 15, 'RUB', 11500.00, 15, 1, 23, 1, 'First'),
(25, 16, 'RUB', 6200.00, 38, 1, 23, 0, 'Economy'),
(26, 16, 'RUB', 11000.00, 19, 1, 23, 1, 'Business'),
(27, 17, 'RUB', 5800.00, 40, 1, 23, 0, 'Economy'),
(28, 17, 'RUB', 10500.00, 20, 1, 23, 1, 'Business'),
(29, 18, 'RUB', 6000.00, 36, 1, 23, 0, 'Economy'),
(30, 18, 'RUB', 12000.00, 14, 1, 32, 1, 'First'),
(31, 19, 'RUB', 5200.00, 42, 1, 23, 0, 'Economy'),
(32, 19, 'RUB', 10000.00, 22, 1, 23, 1, 'Business'),
(33, 20, 'RUB', 5600.00, 39, 1, 23, 0, 'Economy'),
(34, 20, 'RUB', 11000.00, 19, 1, 23, 1, 'Business'),
(35, 21, 'RUB', 4800.00, 51, 1, 23, 0, 'Economy'),
(36, 21, 'RUB', 9200.00, 26, 1, 23, 1, 'Business'),
(37, 22, 'RUB', 5000.00, 45, 1, 23, 0, 'Economy'),
(38, 22, 'RUB', 10500.00, 17, 1, 23, 1, 'Business'),
(39, 23, 'RUB', 6200.00, 34, 1, 23, 0, 'Economy'),
(40, 23, 'RUB', 11800.00, 16, 1, 32, 1, 'First'),
(41, 24, 'RUB', 4900.00, 49, 1, 23, 0, 'Economy'),
(42, 24, 'RUB', 9600.00, 24, 1, 23, 1, 'Business'),
(43, 25, 'RUB', 5400.00, 41, 1, 23, 0, 'Economy'),
(44, 25, 'RUB', 10800.00, 18, 1, 23, 1, 'Business'),
(45, 26, 'RUB', 5100.00, 47, 1, 23, 0, 'Economy'),
(46, 26, 'RUB', 10000.00, 21, 1, 23, 1, 'Business'),
(47, 27, 'RUB', 4800.00, 53, 1, 23, 0, 'Economy'),
(48, 27, 'RUB', 9400.00, 27, 1, 23, 1, 'Business'),
(49, 28, 'RUB', 6300.00, 33, 1, 23, 0, 'Economy'),
(50, 28, 'RUB', 12000.00, 15, 1, 32, 1, 'First'),
(51, 29, 'RUB', 5700.00, 38, 1, 23, 0, 'Economy'),
(52, 29, 'RUB', 11000.00, 20, 1, 23, 1, 'Business'),
(53, 30, 'RUB', 4700.00, 50, 1, 23, 0, 'Economy'),
(54, 30, 'RUB', 9100.00, 25, 1, 23, 1, 'Business'),
(55, 31, 'RUB', 6100.00, 35, 1, 23, 0, 'Economy'),
(56, 31, 'RUB', 11500.00, 17, 1, 32, 1, 'First'),
(57, 32, 'RUB', 4900.00, 48, 1, 23, 0, 'Economy'),
(58, 32, 'RUB', 9500.00, 24, 1, 23, 1, 'Business'),
(59, 33, 'RUB', 5300.00, 43, 1, 23, 0, 'Economy'),
(60, 33, 'RUB', 10300.00, 22, 1, 23, 1, 'Business'),
(61, 34, 'RUB', 5900.00, 37, 1, 23, 0, 'Economy'),
(62, 34, 'RUB', 11200.00, 18, 1, 32, 1, 'First'),
(63, 35, 'RUB', 5100.00, 46, 1, 23, 0, 'Economy'),
(64, 35, 'RUB', 9700.00, 26, 1, 23, 1, 'Business'),
(65, 36, 'RUB', 6000.00, 36, 1, 23, 0, 'Economy'),
(66, 36, 'RUB', 11400.00, 19, 1, 23, 1, 'Business'),
(67, 37, 'RUB', 4800.00, 52, 1, 23, 0, 'Economy'),
(68, 37, 'RUB', 9200.00, 28, 1, 23, 1, 'Business'),
(69, 38, 'RUB', 5500.00, 40, 1, 23, 0, 'Economy'),
(70, 38, 'RUB', 10500.00, 21, 1, 23, 1, 'Business'),
(71, 39, 'RUB', 6200.00, 34, 1, 23, 0, 'Economy'),
(72, 39, 'RUB', 11800.00, 16, 1, 32, 1, 'First'),
(73, 40, 'RUB', 5000.00, 51, 1, 23, 0, 'Economy'),
(74, 40, 'RUB', 9600.00, 23, 1, 23, 1, 'Business'),
(75, 41, 'RUB', 5800.00, 39, 1, 23, 0, 'Economy'),
(76, 41, 'RUB', 11000.00, 20, 1, 23, 1, 'Business'),
(77, 42, 'RUB', 4700.00, 54, 1, 23, 0, 'Economy'),
(78, 42, 'RUB', 9000.00, 27, 1, 23, 1, 'Business'),
(79, 43, 'RUB', 5600.00, 41, 1, 23, 0, 'Economy'),
(80, 43, 'RUB', 10800.00, 19, 1, 23, 1, 'Business'),
(81, 44, 'RUB', 6300.00, 33, 1, 23, 0, 'Economy'),
(82, 44, 'RUB', 12200.00, 15, 1, 32, 1, 'First'),
(83, 45, 'RUB', 5900.00, 37, 1, 23, 0, 'Economy'),
(84, 45, 'RUB', 11300.00, 18, 1, 23, 1, 'Business'),
(85, 46, 'RUB', 4900.00, 55, 1, 23, 0, 'Economy'),
(86, 46, 'RUB', 9300.00, 29, 1, 23, 1, 'Business'),
(87, 47, 'RUB', 5200.00, 48, 1, 23, 0, 'Economy'),
(88, 47, 'RUB', 10600.00, 19, 1, 23, 1, 'Business'),
(89, 48, 'RUB', 5400.00, 46, 1, 23, 0, 'Economy'),
(90, 48, 'RUB', 10800.00, 18, 1, 23, 1, 'Business'),
(91, 49, 'RUB', 5600.00, 43, 1, 23, 0, 'Economy'),
(92, 49, 'RUB', 11000.00, 17, 1, 32, 1, 'First'),
(93, 50, 'RUB', 6000.00, 40, 1, 23, 0, 'Economy'),
(94, 50, 'RUB', 11800.00, 16, 1, 32, 1, 'First'),
(95, 51, 'RUB', 4800.00, 52, 1, 23, 0, 'Economy'),
(96, 51, 'RUB', 9400.00, 27, 1, 23, 1, 'Business'),
(97, 52, 'RUB', 5500.00, 41, 1, 23, 0, 'Economy'),
(98, 52, 'RUB', 10500.00, 20, 1, 23, 1, 'Business'),
(99, 53, 'RUB', 5800.00, 39, 1, 23, 0, 'Economy'),
(100, 53, 'RUB', 11200.00, 18, 1, 32, 1, 'First'),
(101, 54, 'RUB', 6100.00, 37, 1, 23, 0, 'Economy'),
(102, 54, 'RUB', 11800.00, 17, 1, 32, 1, 'First'),
(103, 55, 'RUB', 4900.00, 54, 1, 23, 0, 'Economy'),
(104, 55, 'RUB', 9600.00, 25, 1, 23, 1, 'Business'),
(105, 56, 'RUB', 5200.00, 47, 1, 23, 0, 'Economy'),
(106, 56, 'RUB', 10200.00, 19, 1, 23, 1, 'Business'),
(107, 57, 'RUB', 6400.00, 35, 1, 23, 0, 'Economy'),
(108, 57, 'RUB', 12500.00, 14, 1, 32, 1, 'First'),
(109, 58, 'RUB', 5600.00, 42, 1, 23, 0, 'Economy'),
(110, 58, 'RUB', 10800.00, 18, 1, 23, 1, 'Business'),
(111, 59, 'RUB', 4700.00, 56, 1, 23, 0, 'Economy'),
(112, 59, 'RUB', 8900.00, 28, 1, 23, 1, 'Business'),
(113, 60, 'RUB', 5400.00, 44, 1, 23, 0, 'Economy'),
(114, 60, 'RUB', 10600.00, 19, 1, 23, 1, 'Business'),
(115, 61, 'RUB', 5800.00, 40, 1, 23, 0, 'Economy'),
(116, 61, 'RUB', 11000.00, 18, 1, 32, 1, 'First'),
(117, 62, 'RUB', 5300.00, 45, 1, 23, 0, 'Economy'),
(118, 62, 'RUB', 10300.00, 20, 1, 23, 1, 'Business'),
(119, 63, 'RUB', 4600.00, 58, 0, 0, 0, 'Economy'),
(120, 63, 'RUB', 8700.00, 30, 1, 23, 1, 'Business'),
(121, 64, 'RUB', 5900.00, 38, 1, 23, 0, 'Economy'),
(122, 64, 'RUB', 11400.00, 17, 1, 32, 1, 'First'),
(123, 65, 'RUB', 6500.00, 36, 1, 23, 0, 'Economy'),
(124, 65, 'RUB', 12600.00, 14, 1, 32, 1, 'First'),
(125, 66, 'RUB', 5100.00, 51, 1, 23, 0, 'Economy'),
(126, 66, 'RUB', 9800.00, 24, 1, 23, 1, 'Business'),
(127, 67, 'RUB', 5700.00, 43, 1, 23, 0, 'Economy'),
(128, 67, 'RUB', 11000.00, 19, 1, 23, 1, 'Business'),
(129, 68, 'RUB', 6300.00, 35, 1, 23, 0, 'Economy'),
(130, 68, 'RUB', 12700.00, 15, 1, 32, 1, 'First'),
(131, 69, 'RUB', 4800.00, 55, 1, 23, 0, 'Economy'),
(132, 69, 'RUB', 9500.00, 27, 1, 23, 1, 'Business'),
(133, 70, 'RUB', 6200.00, 34, 1, 23, 0, 'Economy'),
(134, 70, 'RUB', 12000.00, 16, 1, 32, 1, 'First'),
(135, 71, 'RUB', 5500.00, 48, 1, 23, 0, 'Economy'),
(136, 71, 'RUB', 10500.00, 20, 1, 23, 1, 'Business'),
(137, 72, 'RUB', 4900.00, 54, 1, 23, 0, 'Economy'),
(138, 72, 'RUB', 9600.00, 25, 1, 23, 1, 'Business'),
(139, 73, 'RUB', 5300.00, 46, 1, 23, 0, 'Economy'),
(140, 73, 'RUB', 10400.00, 19, 1, 23, 1, 'Business'),
(141, 74, 'RUB', 5800.00, 41, 1, 23, 0, 'Economy'),
(142, 74, 'RUB', 11200.00, 18, 1, 32, 1, 'First'),
(143, 75, 'RUB', 6000.00, 39, 1, 23, 0, 'Economy'),
(144, 75, 'RUB', 11800.00, 17, 1, 32, 1, 'First'),
(145, 76, 'RUB', 5400.00, 47, 1, 23, 0, 'Economy'),
(146, 76, 'RUB', 10600.00, 20, 1, 23, 1, 'Business'),
(147, 77, 'RUB', 5900.00, 42, 1, 23, 0, 'Economy'),
(148, 77, 'RUB', 11400.00, 19, 1, 23, 1, 'Business'),
(149, 78, 'RUB', 6600.00, 33, 1, 23, 0, 'Economy'),
(150, 78, 'RUB', 13000.00, 14, 1, 32, 1, 'First'),
(151, 79, 'RUB', 5200.00, 50, 1, 23, 0, 'Economy'),
(152, 79, 'RUB', 10200.00, 22, 1, 23, 1, 'Business'),
(153, 80, 'RUB', 5600.00, 44, 1, 23, 0, 'Economy'),
(154, 80, 'RUB', 11000.00, 19, 1, 23, 1, 'Business'),
(155, 81, 'RUB', 5000.00, 53, 1, 23, 0, 'Economy'),
(156, 81, 'RUB', 9700.00, 26, 1, 23, 1, 'Business'),
(157, 82, 'RUB', 5400.00, 48, 1, 23, 0, 'Economy'),
(158, 82, 'RUB', 10600.00, 20, 1, 23, 1, 'Business'),
(159, 83, 'RUB', 5800.00, 41, 1, 23, 0, 'Economy'),
(160, 83, 'RUB', 11200.00, 18, 1, 32, 1, 'First'),
(161, 84, 'RUB', 4900.00, 55, 1, 23, 0, 'Economy'),
(162, 84, 'RUB', 9600.00, 27, 1, 23, 1, 'Business'),
(163, 85, 'RUB', 5200.00, 49, 1, 23, 0, 'Economy'),
(164, 85, 'RUB', 10400.00, 21, 1, 23, 1, 'Business'),
(165, 86, 'RUB', 5700.00, 43, 1, 23, 0, 'Economy'),
(166, 86, 'RUB', 11000.00, 19, 1, 23, 1, 'Business'),
(167, 87, 'RUB', 6100.00, 38, 1, 23, 0, 'Economy'),
(168, 87, 'RUB', 12100.00, 15, 1, 32, 1, 'First'),
(169, 88, 'RUB', 5500.00, 46, 1, 23, 0, 'Economy'),
(170, 88, 'RUB', 10700.00, 20, 1, 23, 1, 'Business'),
(171, 89, 'RUB', 5900.00, 40, 1, 23, 0, 'Economy'),
(172, 89, 'RUB', 11400.00, 18, 1, 32, 1, 'First'),
(173, 90, 'RUB', 5300.00, 52, 1, 23, 0, 'Economy'),
(174, 90, 'RUB', 10300.00, 23, 1, 23, 1, 'Business'),
(175, 91, 'RUB', 6400.00, 36, 1, 23, 0, 'Economy'),
(176, 91, 'RUB', 12800.00, 14, 1, 32, 1, 'First'),
(177, 92, 'RUB', 5700.00, 45, 1, 23, 0, 'Economy'),
(178, 92, 'RUB', 11100.00, 19, 1, 23, 1, 'Business'),
(179, 93, 'RUB', 5400.00, 50, 1, 23, 0, 'Economy'),
(180, 93, 'RUB', 10600.00, 22, 1, 23, 1, 'Business'),
(181, 94, 'RUB', 5800.00, 23, 1, 23, 0, 'Economy'),
(182, 94, 'RUB', 11000.00, 18, 1, 23, 1, 'Business'),
(183, 95, 'RUB', 6200.00, 35, 1, 23, 0, 'Economy'),
(184, 95, 'RUB', 12400.00, 14, 1, 32, 1, 'First'),
(185, 96, 'RUB', 5700.00, 41, 1, 23, 0, 'Economy'),
(186, 96, 'RUB', 11100.00, 19, 1, 23, 1, 'Business'),
(187, 97, 'RUB', 6400.00, 36, 1, 23, 0, 'Economy'),
(188, 97, 'RUB', 12800.00, 15, 1, 32, 1, 'First'),
(189, 98, 'RUB', 5400.00, 48, 1, 23, 0, 'Economy'),
(190, 98, 'RUB', 10800.00, 21, 1, 23, 1, 'Business'),
(191, 99, 'RUB', 5900.00, 42, 1, 23, 0, 'Economy'),
(192, 99, 'RUB', 11600.00, 17, 1, 32, 1, 'First'),
(193, 100, 'RUB', 5600.00, 45, 1, 23, 0, 'Economy'),
(194, 100, 'RUB', 11200.00, 20, 1, 23, 1, 'Business'),
(195, 101, 'RUB', 5300.00, 50, 1, 23, 0, 'Economy'),
(196, 101, 'RUB', 10600.00, 22, 1, 23, 1, 'Business'),
(197, 102, 'RUB', 5800.00, 40, 1, 23, 0, 'Economy'),
(198, 102, 'RUB', 11400.00, 19, 1, 23, 1, 'Business'),
(199, 103, 'RUB', 6100.00, 37, 1, 23, 0, 'Economy'),
(200, 103, 'RUB', 12500.00, 14, 1, 32, 1, 'First'),
(201, 104, 'RUB', 5500.00, 46, 1, 23, 0, 'Economy'),
(202, 104, 'RUB', 11000.00, 20, 1, 23, 1, 'Business'),
(203, 105, 'RUB', 6000.00, 38, 1, 23, 0, 'Economy'),
(204, 105, 'RUB', 12200.00, 16, 1, 32, 1, 'First'),
(205, 106, 'RUB', 5700.00, 43, 1, 23, 0, 'Economy'),
(206, 106, 'RUB', 11300.00, 18, 1, 23, 1, 'Business'),
(207, 107, 'RUB', 6300.00, 35, 1, 23, 0, 'Economy'),
(208, 107, 'RUB', 12600.00, 15, 1, 32, 1, 'First'),
(209, 108, 'RUB', 5900.00, 41, 1, 23, 0, 'Economy'),
(210, 108, 'RUB', 11600.00, 17, 1, 32, 1, 'First');

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
INSERT INTO `flights` VALUES (5,'SU112',1,1,1,2,'2025-12-18 22:21:03.000000','2025-12-19 01:21:03.000000',185,'2025-12-18 22:21:03.000000','Scheduled'),(6,'UT328',2,2,1,3,'2025-12-18 22:21:03.000000','2025-12-19 00:21:03.000000',126,'2025-12-18 22:21:03.000000','Scheduled'),(7,'UT128',2,3,1,4,'2025-12-18 22:21:03.000000','2025-12-19 03:21:03.000000',307,'2025-12-18 22:21:03.000000','Scheduled'),(8,'S7115',3,1,1,2,'2025-12-18 22:21:03.000000','2025-12-19 01:21:03.000000',179,'2025-12-18 22:21:03.000000','Scheduled')(9, 'S7598', 3, 1, 8, 6, '2026-05-01 10:15:00.000000', '2026-05-01 13:28:00.000000', 193, '2026-04-26 00:00:00.000000', 'Scheduled'),
(10, 'S7519', 3, 1, 8, 9, '2026-05-02 10:00:00.000000', '2026-05-02 13:22:00.000000', 202, '2026-04-26 00:00:00.000000', 'Scheduled'),
(11, 'S7866', 3, 2, 1, 8, '2026-05-02 18:30:00.000000', '2026-05-02 21:39:00.000000', 189, '2026-04-26 00:00:00.000000', 'Scheduled'),
(12, 'SU280', 1, 1, 3, 6, '2026-05-03 10:15:00.000000', '2026-05-03 12:03:00.000000', 108, '2026-04-26 00:00:00.000000', 'Scheduled'),
(13, 'SU412', 1, 1, 3, 6, '2026-05-03 18:00:00.000000', '2026-05-03 19:46:00.000000', 106, '2026-04-26 00:00:00.000000', 'Scheduled'),
(14, 'UT519', 2, 3, 1, 3, '2026-05-04 06:45:00.000000', '2026-05-04 08:33:00.000000', 108, '2026-04-26 00:00:00.000000', 'Scheduled'),
(15, 'SU607', 1, 2, 2, 9, '2026-05-04 10:30:00.000000', '2026-05-04 15:57:00.000000', 327, '2026-04-26 00:00:00.000000', 'Scheduled'),
(16, 'S7737', 3, 1, 1, 13, '2026-05-04 18:45:00.000000', '2026-05-04 23:11:00.000000', 266, '2026-04-26 00:00:00.000000', 'Scheduled'),
(17, 'UT645', 2, 1, 2, 1, '2026-05-05 10:30:00.000000', '2026-05-05 12:22:00.000000', 112, '2026-04-26 00:00:00.000000', 'Scheduled'),
(18, 'S7186', 3, 3, 8, 10, '2026-05-05 14:15:00.000000', '2026-05-05 17:00:00.000000', 165, '2026-04-26 00:00:00.000000', 'Scheduled'),
(19, 'S7471', 3, 3, 2, 1, '2026-05-07 06:15:00.000000', '2026-05-07 08:10:00.000000', 115, '2026-04-26 00:00:00.000000', 'Scheduled'),
(20, 'S7179', 3, 2, 8, 7, '2026-05-09 18:45:00.000000', '2026-05-09 21:16:00.000000', 151, '2026-04-26 00:00:00.000000', 'Scheduled'),
(21, 'UT842', 2, 3, 6, 2, '2026-05-10 18:45:00.000000', '2026-05-10 20:33:00.000000', 108, '2026-04-26 00:00:00.000000', 'Scheduled'),
(22, 'SU502', 1, 3, 3, 8, '2026-05-11 14:00:00.000000', '2026-05-11 17:24:00.000000', 204, '2026-04-26 00:00:00.000000', 'Scheduled'),
(23, 'S7662', 3, 3, 1, 9, '2026-05-12 10:30:00.000000', '2026-05-12 15:24:00.000000', 294, '2026-04-26 00:00:00.000000', 'Scheduled'),
(24, 'SU470', 1, 3, 10, 5, '2026-05-13 14:00:00.000000', '2026-05-13 18:08:00.000000', 248, '2026-04-26 00:00:00.000000', 'Scheduled'),
(25, 'SU738', 1, 3, 5, 3, '2026-05-13 18:15:00.000000', '2026-05-13 20:02:00.000000', 107, '2026-04-26 00:00:00.000000', 'Scheduled'),
(26, 'SU875', 1, 2, 8, 6, '2026-05-14 10:15:00.000000', '2026-05-14 13:26:00.000000', 191, '2026-04-26 00:00:00.000000', 'Scheduled'),
(27, 'UT507', 2, 3, 3, 1, '2026-05-14 14:15:00.000000', '2026-05-14 15:57:00.000000', 102, '2026-04-26 00:00:00.000000', 'Scheduled'),
(28, 'S7204', 3, 1, 12, 6, '2026-05-15 06:15:00.000000', '2026-05-15 10:41:00.000000', 266, '2026-04-26 00:00:00.000000', 'Scheduled'),
(29, 'SU237', 1, 1, 5, 14, '2026-05-16 10:00:00.000000', '2026-05-16 20:28:00.000000', 628, '2026-04-26 00:00:00.000000', 'Scheduled'),
(30, 'SU799', 1, 1, 9, 1, '2026-05-17 06:00:00.000000', '2026-05-17 11:01:00.000000', 301, '2026-04-26 00:00:00.000000', 'Scheduled'),
(31, 'S7570', 3, 3, 9, 8, '2026-05-17 10:15:00.000000', '2026-05-17 13:34:00.000000', 199, '2026-04-26 00:00:00.000000', 'Scheduled'),
(32, 'SU319', 1, 3, 8, 5, '2026-05-17 14:00:00.000000', '2026-05-17 17:06:00.000000', 186, '2026-04-26 00:00:00.000000', 'Scheduled'),
(33, 'S7619', 3, 3, 9, 8, '2026-05-18 06:00:00.000000', '2026-05-18 09:08:00.000000', 188, '2026-04-26 00:00:00.000000', 'Scheduled'),
(34, 'SU791', 1, 2, 10, 6, '2026-05-18 14:30:00.000000', '2026-05-18 18:53:00.000000', 263, '2026-04-26 00:00:00.000000', 'Scheduled'),
(35, 'UT722', 2, 2, 7, 5, '2026-05-18 18:30:00.000000', '2026-05-18 21:01:00.000000', 151, '2026-04-26 00:00:00.000000', 'Scheduled'),
(36, 'S7169', 3, 3, 8, 7, '2026-05-19 14:30:00.000000', '2026-05-19 17:03:00.000000', 153, '2026-04-26 00:00:00.000000', 'Scheduled'),
(37, 'UT526', 2, 3, 1, 3, '2026-05-20 10:15:00.000000', '2026-05-20 11:59:00.000000', 104, '2026-04-26 00:00:00.000000', 'Scheduled'),
(38, 'SU662', 1, 1, 5, 8, '2026-05-20 18:45:00.000000', '2026-05-20 21:49:00.000000', 184, '2026-04-26 00:00:00.000000', 'Scheduled'),
(39, 'SU299', 1, 1, 5, 13, '2026-05-22 10:45:00.000000', '2026-05-22 15:02:00.000000', 257, '2026-04-26 00:00:00.000000', 'Scheduled'),
(40, 'S7274', 3, 2, 8, 4, '2026-05-25 14:45:00.000000', '2026-05-25 17:08:00.000000', 143, '2026-04-26 00:00:00.000000', 'Scheduled'),
(41, 'S7916', 3, 1, 4, 1, '2026-05-25 18:45:00.000000', '2026-05-25 21:22:00.000000', 157, '2026-04-26 00:00:00.000000', 'Scheduled'),
(42, 'SU228', 1, 2, 5, 4, '2026-05-26 06:30:00.000000', '2026-05-26 08:57:00.000000', 147, '2026-04-26 00:00:00.000000', 'Scheduled'),
(43, 'S7698', 3, 1, 8, 7, '2026-05-28 14:30:00.000000', '2026-05-28 17:04:00.000000', 154, '2026-04-26 00:00:00.000000', 'Scheduled'),
(44, 'SU832', 1, 1, 10, 5, '2026-05-29 10:45:00.000000', '2026-05-29 15:06:00.000000', 261, '2026-04-26 00:00:00.000000', 'Scheduled'),
(45, 'SU377', 1, 2, 7, 1, '2026-05-29 18:15:00.000000', '2026-05-29 20:56:00.000000', 161, '2026-04-26 00:00:00.000000', 'Scheduled'),
(46, 'SU243', 1, 1, 3, 8, '2026-05-30 18:15:00.000000', '2026-05-30 21:42:00.000000', 207, '2026-04-26 00:00:00.000000', 'Scheduled'),
(47, 'SU236', 1, 2, 5, 14, '2026-05-31 06:30:00.000000', '2026-05-31 16:54:00.000000', 624, '2026-04-26 00:00:00.000000', 'Scheduled'),
(48, 'SU593', 1, 3, 8, 2, '2026-05-31 18:30:00.000000', '2026-05-31 22:13:00.000000', 223, '2026-04-26 00:00:00.000000', 'Scheduled'),
(49, 'S7401', 3, 2, 8, 2, '2026-06-01 14:15:00.000000', '2026-06-01 17:47:00.000000', 212, '2026-04-26 00:00:00.000000', 'Scheduled'),
(50, 'S7848', 3, 3, 1, 8, '2026-06-02 10:30:00.000000', '2026-06-02 13:32:00.000000', 182, '2026-04-26 00:00:00.000000', 'Scheduled'),
(51, 'S7748', 3, 3, 1, 2, '2026-06-06 18:30:00.000000', '2026-06-06 20:22:00.000000', 112, '2026-04-26 00:00:00.000000', 'Scheduled'),
(52, 'SU359', 1, 2, 9, 14, '2026-06-09 14:45:00.000000', '2026-06-09 21:14:00.000000', 389, '2026-04-26 00:00:00.000000', 'Scheduled'),
(53, 'S7754', 3, 3, 1, 12, '2026-06-09 18:45:00.000000', '2026-06-09 23:10:00.000000', 265, '2026-04-26 00:00:00.000000', 'Scheduled'),
(54, 'S7556', 3, 3, 1, 13, '2026-06-13 14:45:00.000000', '2026-06-13 19:03:00.000000', 258, '2026-04-26 00:00:00.000000', 'Scheduled'),
(55, 'SU936', 1, 2, 3, 6, '2026-06-15 10:15:00.000000', '2026-06-15 11:52:00.000000', 97, '2026-04-26 00:00:00.000000', 'Scheduled'),
(56, 'UT475', 2, 3, 3, 6, '2026-06-16 06:00:00.000000', '2026-06-16 07:42:00.000000', 102, '2026-04-26 00:00:00.000000', 'Scheduled'),
(57, 'SU937', 1, 2, 2, 9, '2026-06-16 14:00:00.000000', '2026-06-16 19:21:00.000000', 321, '2026-04-26 00:00:00.000000', 'Scheduled'),
(58, 'UT614', 2, 2, 2, 6, '2026-06-17 18:30:00.000000', '2026-06-17 20:19:00.000000', 109, '2026-04-26 00:00:00.000000', 'Scheduled'),
(59, 'S7779', 3, 2, 4, 5, '2026-06-18 10:00:00.000000', '2026-06-18 12:28:00.000000', 148, '2026-04-26 00:00:00.000000', 'Scheduled'),
(60, 'S7760', 3, 3, 8, 9, '2026-06-18 14:45:00.000000', '2026-06-18 17:57:00.000000', 192, '2026-04-26 00:00:00.000000', 'Scheduled'),
(61, 'S7714', 3, 3, 8, 13, '2026-06-19 18:45:00.000000', '2026-06-19 21:50:00.000000', 185, '2026-04-26 00:00:00.000000', 'Scheduled'),
(62, 'SU523', 1, 3, 5, 15, '2026-06-20 06:00:00.000000', '2026-06-20 16:47:00.000000', 647, '2026-04-26 00:00:00.000000', 'Scheduled'),
(63, 'S7130', 3, 1, 4, 5, '2026-06-22 06:45:00.000000', '2026-06-22 09:26:00.000000', 161, '2026-04-26 00:00:00.000000', 'Scheduled'),
(64, 'SU955', 1, 2, 5, 10, '2026-06-23 06:30:00.000000', '2026-06-23 10:39:00.000000', 249, '2026-04-26 00:00:00.000000', 'Scheduled'),
(65, 'SU837', 1, 1, 10, 1, '2026-06-24 10:30:00.000000', '2026-06-24 14:48:00.000000', 258, '2026-04-26 00:00:00.000000', 'Scheduled'),
(66, 'SU709', 1, 2, 9, 14, '2026-06-26 06:15:00.000000', '2026-06-26 12:46:00.000000', 391, '2026-04-26 00:00:00.000000', 'Scheduled'),
(67, 'S7966', 3, 2, 4, 8, '2026-06-27 18:00:00.000000', '2026-06-27 20:25:00.000000', 145, '2026-04-26 00:00:00.000000', 'Scheduled'),
(68, 'SU135', 1, 3, 9, 14, '2026-06-28 06:30:00.000000', '2026-06-28 13:02:00.000000', 392, '2026-04-26 00:00:00.000000', 'Scheduled'),
(69, 'S7697', 3, 3, 8, 11, '2026-06-29 06:45:00.000000', '2026-06-29 09:30:00.000000', 165, '2026-04-26 00:00:00.000000', 'Scheduled'),
(70, 'SU420', 1, 3, 5, 17, '2026-06-29 10:00:00.000000', '2026-06-29 20:45:00.000000', 645, '2026-04-26 00:00:00.000000', 'Scheduled'),
(71, 'SU814', 1, 2, 4, 1, '2026-06-29 18:45:00.000000', '2026-06-29 21:20:00.000000', 155, '2026-04-26 00:00:00.000000', 'Scheduled'),
(72, 'SU308', 1, 1, 12, 1, '2026-06-30 14:00:00.000000', '2026-06-30 18:29:00.000000', 269, '2026-04-26 00:00:00.000000', 'Scheduled'),
(73, 'SU629', 1, 3, 2, 6, '2026-07-01 06:00:00.000000', '2026-07-01 07:50:00.000000', 110, '2026-04-26 00:00:00.000000', 'Scheduled'),
(74, 'SU819', 1, 2, 9, 14, '2026-07-01 18:00:00.000000', '2026-07-02 00:32:00.000000', 392, '2026-04-26 00:00:00.000000', 'Scheduled'),
(75, 'SU107', 1, 3, 9, 14, '2026-07-02 10:15:00.000000', '2026-07-02 16:34:00.000000', 379, '2026-04-26 00:00:00.000000', 'Scheduled'),
(76, 'UT249', 2, 3, 7, 1, '2026-07-04 18:15:00.000000', '2026-07-04 20:58:00.000000', 163, '2026-04-26 00:00:00.000000', 'Scheduled'),
(77, 'UT431', 2, 2, 6, 3, '2026-07-08 06:30:00.000000', '2026-07-08 08:19:00.000000', 109, '2026-04-26 00:00:00.000000', 'Scheduled'),
(78, 'SU711', 1, 1, 13, 6, '2026-07-08 14:00:00.000000', '2026-07-08 18:28:00.000000', 268, '2026-04-26 00:00:00.000000', 'Scheduled'),
(79, 'SU339', 1, 3, 3, 8, '2026-07-08 18:30:00.000000', '2026-07-08 21:59:00.000000', 209, '2026-04-26 00:00:00.000000', 'Scheduled'),
(80, 'SU751', 1, 2, 2, 9, '2026-07-09 18:45:00.000000', '2026-07-10 00:13:00.000000', 328, '2026-04-26 00:00:00.000000', 'Scheduled'),
(81, 'UT369', 2, 1, 6, 2, '2026-07-10 06:15:00.000000', '2026-07-10 08:07:00.000000', 112, '2026-04-26 00:00:00.000000', 'Scheduled'),
(82, 'SU464', 1, 1, 8, 1, '2026-07-11 06:30:00.000000', '2026-07-11 09:33:00.000000', 183, '2026-04-26 00:00:00.000000', 'Scheduled'),
(83, 'S7423', 3, 1, 8, 10, '2026-07-12 14:30:00.000000', '2026-07-12 17:21:00.000000', 171, '2026-04-26 00:00:00.000000', 'Scheduled'),
(84, 'UT316', 2, 1, 3, 5, '2026-07-12 18:00:00.000000', '2026-07-12 19:50:00.000000', 110, '2026-04-26 00:00:00.000000', 'Scheduled'),
(85, 'SU143', 1, 3, 2, 9, '2026-07-13 14:45:00.000000', '2026-07-13 20:01:00.000000', 316, '2026-04-26 00:00:00.000000', 'Scheduled'),
(86, 'UT176', 2, 3, 7, 6, '2026-07-14 06:30:00.000000', '2026-07-14 09:13:00.000000', 163, '2026-04-26 00:00:00.000000', 'Scheduled'),
(87, 'SU900', 1, 2, 13, 1, '2026-07-15 14:30:00.000000', '2026-07-15 19:02:00.000000', 272, '2026-04-26 00:00:00.000000', 'Scheduled'),
(88, 'S7818', 3, 1, 2, 6, '2026-07-17 06:30:00.000000', '2026-07-17 08:15:00.000000', 105, '2026-04-26 00:00:00.000000', 'Scheduled'),
(89, 'SU771', 1, 1, 3, 8, '2026-07-17 10:15:00.000000', '2026-07-17 13:44:00.000000', 209, '2026-04-26 00:00:00.000000', 'Scheduled'),
(90, 'S7588', 3, 2, 2, 1, '2026-07-17 18:15:00.000000', '2026-07-17 19:59:00.000000', 104, '2026-04-26 00:00:00.000000', 'Scheduled'),
(91, 'SU218', 1, 3, 10, 6, '2026-07-18 18:45:00.000000', '2026-07-18 23:07:00.000000', 262, '2026-04-26 00:00:00.000000', 'Scheduled'),
(92, 'S7982', 3, 1, 8, 9, '2026-07-19 06:30:00.000000', '2026-07-19 09:47:00.000000', 197, '2026-04-26 00:00:00.000000', 'Scheduled'),
(93, 'SU180', 1, 3, 5, 3, '2026-07-19 10:30:00.000000', '2026-07-19 12:20:00.000000', 110, '2026-04-26 00:00:00.000000', 'Scheduled'),
(94, 'SU277', 1, 3, 5, 3, '2026-07-22 06:15:00.000000', '2026-07-22 07:59:00.000000', 104, '2026-04-26 00:00:00.000000', 'Scheduled'),
(95, 'SU448', 1, 1, 2, 5, '2026-07-22 10:30:00.000000', '2026-07-22 12:22:00.000000', 112, '2026-04-26 00:00:00.000000', 'Scheduled'),
(96, 'SU874', 1, 2, 5, 15, '2026-07-23 06:45:00.000000', '2026-07-23 17:17:00.000000', 632, '2026-04-26 00:00:00.000000', 'Scheduled'),
(97, 'SU388', 1, 3, 3, 8, '2026-07-23 18:15:00.000000', '2026-07-23 21:39:00.000000', 204, '2026-04-26 00:00:00.000000', 'Scheduled'),
(98, 'SU324', 1, 2, 5, 9, '2026-07-24 06:15:00.000000', '2026-07-24 11:20:00.000000', 305, '2026-04-26 00:00:00.000000', 'Scheduled'),
(99, 'SU286', 1, 3, 8, 1, '2026-07-24 10:45:00.000000', '2026-07-24 14:02:00.000000', 197, '2026-04-26 00:00:00.000000', 'Scheduled'),
(100, 'SU693', 1, 1, 13, 1, '2026-07-25 06:30:00.000000', '2026-07-25 11:00:00.000000', 270, '2026-04-26 00:00:00.000000', 'Scheduled'),
(101, 'S7231', 3, 3, 4, 1, '2026-07-26 10:15:00.000000', '2026-07-26 12:57:00.000000', 162, '2026-04-26 00:00:00.000000', 'Scheduled'),
(102, 'S7659', 3, 3, 9, 8, '2026-07-27 06:00:00.000000', '2026-07-27 09:15:00.000000', 195, '2026-04-26 00:00:00.000000', 'Scheduled'),
(103, 'SU713', 1, 2, 5, 4, '2026-07-27 14:45:00.000000', '2026-07-27 17:20:00.000000', 155, '2026-04-26 00:00:00.000000', 'Scheduled'),
(104, 'S7430', 3, 2, 9, 8, '2026-07-28 14:15:00.000000', '2026-07-28 17:27:00.000000', 192, '2026-04-26 00:00:00.000000', 'Scheduled'),
(105, 'SU425', 1, 2, 5, 17, '2026-07-29 06:15:00.000000', '2026-07-29 16:53:00.000000', 638, '2026-04-26 00:00:00.000000', 'Scheduled'),
(106, 'S7849', 3, 2, 12, 6, '2026-07-29 14:30:00.000000', '2026-07-29 18:47:00.000000', 257, '2026-04-26 00:00:00.000000', 'Scheduled'),
(107, 'SU336', 1, 2, 3, 8, '2026-07-29 18:15:00.000000', '2026-07-29 21:40:00.000000', 205, '2026-04-26 00:00:00.000000', 'Scheduled'),
(108, 'SU535', 1, 1, 5, 15, '2026-07-30 10:00:00.000000', '2026-07-30 20:37:00.000000', 637, '2026-04-26 00:00:00.000000', 'Scheduled');
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
