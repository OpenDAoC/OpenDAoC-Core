/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `timexlevel` (
  `TimeXLevel_ID` int(11) NOT NULL AUTO_INCREMENT,
  `Character_ID` text NOT NULL,
  `Character_Name` text NOT NULL,
  `Character_Realm` int(11) NOT NULL DEFAULT 0,
  `Character_Class` text NOT NULL,
  `Character_Level` int(11) NOT NULL DEFAULT 0,
  `Hardcore` int(11) NOT NULL DEFAULT 0,
  `TimeToLevel` text NOT NULL,
  `SecondsToLevel` bigint(20) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `HoursToLevel` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`TimeXLevel_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

/*!40000 ALTER TABLE `timexlevel` DISABLE KEYS */;
/*!40000 ALTER TABLE `timexlevel` ENABLE KEYS */;

