/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `playerboats` (
  `BoatID` text NOT NULL,
  `BoatOwner` text NOT NULL,
  `BoatName` varchar(255) NOT NULL,
  `BoatModel` smallint(5) unsigned NOT NULL DEFAULT 0,
  `BoatMaxSpeedBase` smallint(6) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `PlayerBoats_ID` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`BoatName`),
  UNIQUE KEY `U_PlayerBoats_PlayerBoats_ID` (`PlayerBoats_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `playerboats` DISABLE KEYS */;
/*!40000 ALTER TABLE `playerboats` ENABLE KEYS */;

