-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: localhost    Database: turnirsystem
-- ------------------------------------------------------
-- Server version	9.7.1

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
SET @MYSQLDUMP_TEMP_LOG_BIN = @@SESSION.SQL_LOG_BIN;
SET @@SESSION.SQL_LOG_BIN= 0;

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
INSERT INTO `__efmigrationshistory` VALUES ('20261122143323_PocetnaSema','9.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ekipe`
--

DROP TABLE IF EXISTS `ekipe`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ekipe` (
  `EkipaId` int NOT NULL AUTO_INCREMENT,
  `Naziv` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SkraceniNaziv` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Mesto` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `KontaktOsoba` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `LogoUrl` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`EkipaId`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ekipe`
--

LOCK TABLES `ekipe` WRITE;
/*!40000 ALTER TABLE `ekipe` DISABLE KEYS */;
INSERT INTO `ekipe` VALUES (1,'Test Ekipa 1','T1','Demo',NULL,NULL),(2,'Test Ekipa 2','T2','Demo',NULL,NULL),(3,'Test Ekipa 3','T3','Demo',NULL,NULL),(4,'Test Ekipa 4','T4','Demo',NULL,NULL),(5,'Test Ekipa 5','T5','Demo',NULL,NULL),(6,'Test Ekipa 6','T6','Demo',NULL,NULL),(7,'Test Ekipa 7','T7','Demo',NULL,NULL),(8,'Test Ekipa 8','T8','Demo',NULL,NULL),(9,'Test Ekipa 9','T9','Demo',NULL,NULL),(10,'Test Ekipa 10','T10','Demo',NULL,NULL),(11,'Test Ekipa 11','T11','Demo',NULL,NULL),(12,'Test Ekipa 12','T12','Demo',NULL,NULL);
/*!40000 ALTER TABLE `ekipe` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `eliminacionakola`
--

DROP TABLE IF EXISTS `eliminacionakola`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `eliminacionakola` (
  `EliminacionoKoloId` int NOT NULL AUTO_INCREMENT,
  `TurnirId` int NOT NULL,
  `BrojKola` int NOT NULL,
  `Pozicija` int NOT NULL,
  `JeBye` tinyint(1) NOT NULL,
  `Ekipa1Id` int DEFAULT NULL,
  `Ekipa2Id` int DEFAULT NULL,
  `PobednikId` int DEFAULT NULL,
  `MecId` int DEFAULT NULL,
  `RezultatEkipa1` int DEFAULT NULL,
  `RezultatEkipa2` int DEFAULT NULL,
  `DatumVreme` datetime(6) DEFAULT NULL,
  `Odlozen` tinyint(1) NOT NULL,
  PRIMARY KEY (`EliminacionoKoloId`),
  KEY `IX_EliminacionaKola_Ekipa1Id` (`Ekipa1Id`),
  KEY `IX_EliminacionaKola_Ekipa2Id` (`Ekipa2Id`),
  KEY `IX_EliminacionaKola_MecId` (`MecId`),
  KEY `IX_EliminacionaKola_PobednikId` (`PobednikId`),
  KEY `IX_EliminacionaKola_TurnirId` (`TurnirId`),
  CONSTRAINT `FK_EliminacionaKola_Ekipe_Ekipa1Id` FOREIGN KEY (`Ekipa1Id`) REFERENCES `ekipe` (`EkipaId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_EliminacionaKola_Ekipe_Ekipa2Id` FOREIGN KEY (`Ekipa2Id`) REFERENCES `ekipe` (`EkipaId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_EliminacionaKola_Ekipe_PobednikId` FOREIGN KEY (`PobednikId`) REFERENCES `ekipe` (`EkipaId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_EliminacionaKola_Mecevi_MecId` FOREIGN KEY (`MecId`) REFERENCES `mecevi` (`MecId`),
  CONSTRAINT `FK_EliminacionaKola_Turniri_TurnirId` FOREIGN KEY (`TurnirId`) REFERENCES `turniri` (`TurnirId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `eliminacionakola`
--

LOCK TABLES `eliminacionakola` WRITE;
/*!40000 ALTER TABLE `eliminacionakola` DISABLE KEYS */;
INSERT INTO `eliminacionakola` VALUES (1,2,2,1,0,5,2,2,NULL,2,3,NULL,0),(2,2,2,2,0,6,7,7,NULL,2,3,NULL,0),(3,2,1,1,0,2,7,7,NULL,2,3,NULL,0),(4,2,0,1,0,5,6,5,NULL,3,1,NULL,0),(5,3,3,1,1,6,NULL,6,NULL,NULL,NULL,NULL,0),(6,3,3,2,0,4,1,4,NULL,3,1,NULL,0),(7,3,3,3,1,5,NULL,5,NULL,NULL,NULL,NULL,0),(8,3,3,4,0,2,8,8,NULL,1,3,NULL,0),(9,3,2,1,0,6,4,4,NULL,1,3,NULL,0),(10,3,2,2,0,5,8,5,NULL,3,1,NULL,0),(11,3,1,1,0,4,5,5,NULL,0,3,NULL,0),(12,3,0,1,0,6,8,6,NULL,3,2,NULL,0),(13,5,3,1,0,5,10,10,NULL,2,3,NULL,0),(14,5,3,2,0,9,1,9,NULL,3,1,NULL,0),(15,5,3,3,0,8,6,8,NULL,3,1,NULL,0),(16,5,3,4,0,4,7,4,NULL,3,2,NULL,0),(17,5,2,1,0,10,9,9,NULL,2,3,NULL,0),(18,5,2,2,0,8,4,4,NULL,2,3,NULL,0),(19,5,1,1,0,9,4,9,NULL,3,0,NULL,0),(20,5,0,1,0,10,8,10,NULL,3,1,NULL,0);
/*!40000 ALTER TABLE `eliminacionakola` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `grupaekipe`
--

DROP TABLE IF EXISTS `grupaekipe`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `grupaekipe` (
  `GrupaEkipaId` int NOT NULL AUTO_INCREMENT,
  `GrupaId` int NOT NULL,
  `EkipaId` int NOT NULL,
  `RucniPlasman` int DEFAULT NULL,
  PRIMARY KEY (`GrupaEkipaId`),
  UNIQUE KEY `IX_GrupaEkipe_GrupaId_EkipaId` (`GrupaId`,`EkipaId`),
  KEY `IX_GrupaEkipe_EkipaId` (`EkipaId`),
  CONSTRAINT `FK_GrupaEkipe_Ekipe_EkipaId` FOREIGN KEY (`EkipaId`) REFERENCES `ekipe` (`EkipaId`) ON DELETE CASCADE,
  CONSTRAINT `FK_GrupaEkipe_Grupe_GrupaId` FOREIGN KEY (`GrupaId`) REFERENCES `grupe` (`GrupaId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=43 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `grupaekipe`
--

LOCK TABLES `grupaekipe` WRITE;
/*!40000 ALTER TABLE `grupaekipe` DISABLE KEYS */;
INSERT INTO `grupaekipe` VALUES (1,1,1,NULL),(2,1,3,NULL),(3,1,5,NULL),(4,1,7,NULL),(5,2,2,NULL),(6,2,4,NULL),(7,2,6,NULL),(8,2,8,NULL),(9,3,4,NULL),(10,3,3,NULL),(11,3,2,NULL),(12,3,12,NULL),(13,4,7,NULL),(14,4,11,NULL),(15,4,6,NULL),(16,4,8,NULL),(17,5,5,NULL),(18,5,10,NULL),(19,5,1,NULL),(20,5,9,NULL),(33,10,1,NULL),(34,10,3,NULL),(35,10,4,NULL),(36,11,2,NULL),(37,11,5,NULL),(38,11,7,NULL),(39,12,8,NULL),(40,12,10,NULL),(41,13,6,NULL),(42,13,9,NULL);
/*!40000 ALTER TABLE `grupaekipe` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `grupe`
--

DROP TABLE IF EXISTS `grupe`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `grupe` (
  `GrupaId` int NOT NULL AUTO_INCREMENT,
  `TurnirId` int NOT NULL,
  `Oznaka` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`GrupaId`),
  KEY `IX_Grupe_TurnirId` (`TurnirId`),
  CONSTRAINT `FK_Grupe_Turniri_TurnirId` FOREIGN KEY (`TurnirId`) REFERENCES `turniri` (`TurnirId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `grupe`
--

LOCK TABLES `grupe` WRITE;
/*!40000 ALTER TABLE `grupe` DISABLE KEYS */;
INSERT INTO `grupe` VALUES (1,2,'A'),(2,2,'B'),(3,3,'A'),(4,3,'B'),(5,3,'C'),(10,5,'A'),(11,5,'B'),(12,5,'C'),(13,5,'D');
/*!40000 ALTER TABLE `grupe` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `korisnici`
--

DROP TABLE IF EXISTS `korisnici`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `korisnici` (
  `KorisnikId` int NOT NULL AUTO_INCREMENT,
  `Username` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PasswordHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Rola` int NOT NULL,
  `DatumRegistracije` datetime(6) NOT NULL,
  PRIMARY KEY (`KorisnikId`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `korisnici`
--

LOCK TABLES `korisnici` WRITE;
/*!40000 ALTER TABLE `korisnici` DISABLE KEYS */;
/*!40000 ALTER TABLE `korisnici` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `mecevi`
--

DROP TABLE IF EXISTS `mecevi`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mecevi` (
  `MecId` int NOT NULL AUTO_INCREMENT,
  `TurnirId` int NOT NULL,
  `GrupaId` int DEFAULT NULL,
  `Ekipa1Id` int NOT NULL,
  `Ekipa2Id` int NOT NULL,
  `RezultatEkipa1` int DEFAULT NULL,
  `RezultatEkipa2` int DEFAULT NULL,
  `Status` int NOT NULL,
  `Faza` int NOT NULL,
  `DatumVreme` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`MecId`),
  KEY `IX_Mecevi_Ekipa1Id` (`Ekipa1Id`),
  KEY `IX_Mecevi_Ekipa2Id` (`Ekipa2Id`),
  KEY `IX_Mecevi_GrupaId` (`GrupaId`),
  KEY `IX_Mecevi_TurnirId` (`TurnirId`),
  CONSTRAINT `FK_Mecevi_Ekipe_Ekipa1Id` FOREIGN KEY (`Ekipa1Id`) REFERENCES `ekipe` (`EkipaId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Mecevi_Ekipe_Ekipa2Id` FOREIGN KEY (`Ekipa2Id`) REFERENCES `ekipe` (`EkipaId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Mecevi_Grupe_GrupaId` FOREIGN KEY (`GrupaId`) REFERENCES `grupe` (`GrupaId`),
  CONSTRAINT `FK_Mecevi_Turniri_TurnirId` FOREIGN KEY (`TurnirId`) REFERENCES `turniri` (`TurnirId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=39 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `mecevi`
--

LOCK TABLES `mecevi` WRITE;
/*!40000 ALTER TABLE `mecevi` DISABLE KEYS */;
INSERT INTO `mecevi` VALUES (1,2,1,1,3,3,1,1,0,NULL),(2,2,1,1,5,2,3,1,0,NULL),(3,2,1,1,7,2,3,1,0,NULL),(4,2,1,3,5,3,2,1,0,NULL),(5,2,1,3,7,2,3,1,0,NULL),(6,2,1,5,7,3,1,1,0,NULL),(7,2,2,2,4,3,2,1,0,NULL),(8,2,2,2,6,1,3,1,0,NULL),(9,2,2,2,8,3,2,1,0,NULL),(10,2,2,4,6,3,1,1,0,NULL),(11,2,2,4,8,2,3,1,0,NULL),(12,2,2,6,8,3,1,1,0,NULL),(13,3,3,2,3,1,3,1,0,NULL),(14,3,3,2,4,3,2,1,0,NULL),(15,3,3,2,12,3,2,1,0,NULL),(16,3,3,3,4,2,3,1,0,NULL),(17,3,3,3,12,2,3,1,0,NULL),(18,3,3,4,12,3,0,1,0,NULL),(19,3,4,6,7,3,2,1,0,NULL),(20,3,4,6,8,3,2,1,0,NULL),(21,3,4,6,11,3,0,1,0,NULL),(22,3,4,7,8,1,3,1,0,NULL),(23,3,4,7,11,3,0,1,0,NULL),(24,3,4,8,11,2,3,1,0,NULL),(25,3,5,1,5,1,3,1,0,NULL),(26,3,5,1,9,3,0,1,0,NULL),(27,3,5,1,10,3,1,1,0,NULL),(28,3,5,5,9,3,1,1,0,NULL),(29,3,5,5,10,3,0,1,0,NULL),(30,3,5,9,10,2,3,1,0,NULL),(31,5,10,1,3,3,2,1,0,NULL),(32,5,10,1,4,2,3,1,0,NULL),(33,5,10,3,4,2,3,1,0,NULL),(34,5,11,2,5,2,3,1,0,NULL),(35,5,11,2,7,1,3,1,0,NULL),(36,5,11,5,7,3,1,1,0,NULL),(37,5,12,8,10,3,1,1,0,NULL),(38,5,13,6,9,2,3,1,0,NULL);
/*!40000 ALTER TABLE `mecevi` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `turnirekipe`
--

DROP TABLE IF EXISTS `turnirekipe`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `turnirekipe` (
  `TurnirEkipaId` int NOT NULL AUTO_INCREMENT,
  `TurnirId` int NOT NULL,
  `EkipaId` int NOT NULL,
  PRIMARY KEY (`TurnirEkipaId`),
  UNIQUE KEY `IX_TurnirEkipe_TurnirId_EkipaId` (`TurnirId`,`EkipaId`),
  KEY `IX_TurnirEkipe_EkipaId` (`EkipaId`),
  CONSTRAINT `FK_TurnirEkipe_Ekipe_EkipaId` FOREIGN KEY (`EkipaId`) REFERENCES `ekipe` (`EkipaId`) ON DELETE CASCADE,
  CONSTRAINT `FK_TurnirEkipe_Turniri_TurnirId` FOREIGN KEY (`TurnirId`) REFERENCES `turniri` (`TurnirId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=45 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `turnirekipe`
--

LOCK TABLES `turnirekipe` WRITE;
/*!40000 ALTER TABLE `turnirekipe` DISABLE KEYS */;
INSERT INTO `turnirekipe` VALUES (1,2,1),(2,2,2),(3,2,3),(4,2,4),(5,2,5),(6,2,6),(7,2,7),(8,2,8),(9,3,1),(10,3,2),(11,3,3),(12,3,4),(13,3,5),(14,3,6),(15,3,7),(16,3,8),(17,3,9),(18,3,10),(19,3,11),(20,3,12),(33,5,1),(34,5,2),(35,5,3),(36,5,4),(37,5,5),(38,5,6),(39,5,7),(40,5,8),(41,5,9),(42,5,10);
/*!40000 ALTER TABLE `turnirekipe` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `turniri`
--

DROP TABLE IF EXISTS `turniri`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `turniri` (
  `TurnirId` int NOT NULL AUTO_INCREMENT,
  `Naziv` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Opis` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DatumPocetka` datetime(6) NOT NULL,
  `DatumZavrsetka` datetime(6) DEFAULT NULL,
  `BrojGrupa` int NOT NULL,
  `BrojEkipaPoGrupi` int NOT NULL,
  `BrojKvalifikovanih` int NOT NULL,
  `BodPobeda` int NOT NULL,
  `BodPoraz` int NOT NULL,
  `Status` int NOT NULL,
  `KosturPotvrdjen` tinyint(1) NOT NULL,
  `GrupnaFazaZakljucana` tinyint(1) NOT NULL,
  PRIMARY KEY (`TurnirId`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `turniri`
--

LOCK TABLES `turniri` WRITE;
/*!40000 ALTER TABLE `turniri` DISABLE KEYS */;
INSERT INTO `turniri` VALUES (2,'Demo 8 ekipa','Prvi test turnir za projekat','2026-11-20 00:00:00.000000','2026-11-21 00:00:00.000000',2,4,2,3,0,2,1,1),(3,'Demo 12 Ekipa','','2026-11-21 00:00:00.000000','2026-11-22 00:00:00.000000',3,4,2,3,0,2,1,1),(5,'Demo 10 ekipa','','2026-11-22 00:00:00.000000','2026-11-23 00:00:00.000000',4,3,2,3,0,2,1,1);
/*!40000 ALTER TABLE `turniri` ENABLE KEYS */;
UNLOCK TABLES;
SET @@SESSION.SQL_LOG_BIN = @MYSQLDUMP_TEMP_LOG_BIN;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-11-25 14:59:43
