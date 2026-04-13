/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `battleground` (
  `RegionID` smallint(5) unsigned NOT NULL DEFAULT 0,
  `MinLevel` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `MaxLevel` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `MaxRealmLevel` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `Battleground_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`Battleground_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `battleground` DISABLE KEYS */;
REPLACE INTO `battleground` (`RegionID`, `MinLevel`, `MaxLevel`, `MaxRealmLevel`, `LastTimeRowUpdated`, `Battleground_ID`) VALUES
	(253, 15, 19, 2, '2000-01-01 00:00:00', 'Abermenai (Level 15-19)'),
	(250, 30, 34, 25, '2000-01-01 00:00:00', 'Caledonia (Level 34-39 - RR3L5)'),
	(165, 45, 49, 45, '2000-01-01 00:00:00', 'Cathal Valley (Level 45-49)'),
	(251, 25, 29, 5, '2000-01-01 00:00:00', 'Murdaigean (Level 25-29)'),
	(252, 20, 24, 10, '2000-01-01 00:00:00', 'Thidranki (Level 20-24 - RR2L0)');
/*!40000 ALTER TABLE `battleground` ENABLE KEYS */;

