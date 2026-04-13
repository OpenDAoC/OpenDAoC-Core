/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `minotaurrelic` (
  `relicSpell` int(11) NOT NULL DEFAULT 0,
  `SpawnLocked` tinyint(1) NOT NULL DEFAULT 0,
  `ProtectorClassType` text NOT NULL,
  `relicTarget` text NOT NULL,
  `Name` text NOT NULL,
  `Model` smallint(5) unsigned NOT NULL DEFAULT 0,
  `SpawnX` int(11) NOT NULL DEFAULT 0,
  `SpawnY` int(11) NOT NULL DEFAULT 0,
  `SpawnZ` int(11) NOT NULL DEFAULT 0,
  `SpawnHeading` int(11) NOT NULL DEFAULT 0,
  `SpawnRegion` int(11) NOT NULL DEFAULT 0,
  `Effect` int(11) NOT NULL DEFAULT 0,
  `RelicID` int(11) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `Minotaurrelic_ID` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`RelicID`),
  UNIQUE KEY `U_Minotaurrelic_Minotaurrelic_ID` (`Minotaurrelic_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `minotaurrelic` DISABLE KEYS */;
/*!40000 ALTER TABLE `minotaurrelic` ENABLE KEYS */;

