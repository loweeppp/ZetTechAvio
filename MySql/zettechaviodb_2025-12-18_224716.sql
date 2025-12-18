-- MySQL dump 10.13  Distrib 9.5.0, for Win64 (x86_64)
--
-- Host: localhost    Database: zettechaviodb
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
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

/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20251118093212_InitialCreate','9.0.0'),('20251125142642_AddAviationTables','9.0.0'),('20251201144448_MakeLogoNullable','9.0.0'),('20251208194845_AddFares','9.0.0'),('20251208200730_FixPriceDecimalPrecision','9.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;

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

/*!40000 ALTER TABLE `aircraft` DISABLE KEYS */;
INSERT INTO `aircraft` VALUES (1,'Boeing 737','Boeing',189,'2025-12-18 22:03:16.000000'),(2,'Airbus A320','Airbus',194,'2025-12-18 22:03:16.000000'),(3,'Boeing 777','Boeing',350,'2025-12-18 22:03:16.000000'),(4,'Airbus A330','Airbus',335,'2025-12-18 22:03:16.000000'),(5,'Embraer E190','Embraer',124,'2025-12-18 22:03:16.000000');
/*!40000 ALTER TABLE `aircraft` ENABLE KEYS */;

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

/*!40000 ALTER TABLE `airlines` DISABLE KEYS */;
INSERT INTO `airlines` VALUES (1,'SU','Aeroflot',NULL,'2025-12-18 22:03:10.000000'),(2,'UT','Utair',NULL,'2025-12-18 22:03:10.000000'),(3,'S7','S7 Airlines',NULL,'2025-12-18 22:03:10.000000');
/*!40000 ALTER TABLE `airlines` ENABLE KEYS */;

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

/*!40000 ALTER TABLE `airports` DISABLE KEYS */;
INSERT INTO `airports` VALUES (1,'DME','Domodedovo International Airport','Moscow','Russia',55.4084,37.9015,'2025-12-18 22:03:16.000000'),(2,'LED','Pulkovo Airport','Saint Petersburg','Russia',59.8011,30.2625,'2025-12-18 22:03:16.000000'),(3,'KZN','Kazan International Airport','Kazan','Russia',55.6064,49.2865,'2025-12-18 22:03:16.000000'),(4,'KRR','Anapa Airport','Sochi','Russia',43.4421,39.947,'2025-12-18 22:03:16.000000');
/*!40000 ALTER TABLE `airports` ENABLE KEYS */;

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
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `fares`
--

/*!40000 ALTER TABLE `fares` DISABLE KEYS */;
/*!40000 ALTER TABLE `fares` ENABLE KEYS */;

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

/*!40000 ALTER TABLE `flights` DISABLE KEYS */;
INSERT INTO `flights` VALUES (5,'SU112',1,1,1,2,'2025-12-18 22:21:03.000000','2025-12-19 01:21:03.000000',180,'2025-12-18 22:21:03.000000','Scheduled'),(6,'UT328',2,2,1,3,'2025-12-18 22:21:03.000000','2025-12-19 00:21:03.000000',120,'2025-12-18 22:21:03.000000','Scheduled'),(7,'UT128',2,3,1,4,'2025-12-18 22:21:03.000000','2025-12-19 03:21:03.000000',300,'2025-12-18 22:21:03.000000','Scheduled'),(8,'S7115',3,1,1,2,'2025-12-18 22:21:03.000000','2025-12-19 01:21:03.000000',180,'2025-12-18 22:21:03.000000','Scheduled');
/*!40000 ALTER TABLE `flights` ENABLE KEYS */;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

/*!40000 ALTER TABLE `users` DISABLE KEYS */;
/*!40000 ALTER TABLE `users` ENABLE KEYS */;

--
-- Dumping routines for database 'zettechaviodb'
--
--
-- WARNING: can't read the INFORMATION_SCHEMA.libraries table. It's most probably an old server 8.0.44.
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-12-18 22:47:24
