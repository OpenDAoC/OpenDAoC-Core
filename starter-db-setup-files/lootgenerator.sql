/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `lootgenerator` (
  `MobName` text DEFAULT NULL,
  `MobGuild` text DEFAULT NULL,
  `MobFaction` text DEFAULT NULL,
  `RegionID` smallint(5) unsigned NOT NULL DEFAULT 0,
  `LootGeneratorClass` text NOT NULL,
  `ExclusivePriority` int(11) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `LootGenerator_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`LootGenerator_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `lootgenerator` DISABLE KEYS */;
REPLACE INTO `lootgenerator` (`MobName`, `MobGuild`, `MobFaction`, `RegionID`, `LootGeneratorClass`, `ExclusivePriority`, `LastTimeRowUpdated`, `LootGenerator_ID`) VALUES
	(NULL, NULL, NULL, 0, 'DOL.GS.ROGMobGenerator', 0, '2000-01-01 00:00:00', 'Atlas ROGs'),
	(NULL, NULL, NULL, 0, 'DOL.GS.LootGeneratorMoney', 0, '2000-01-01 00:00:00', 'money'),
	(NULL, NULL, NULL, 0, 'DOL.GS.LootGeneratorOneTimeDrop', 0, '2000-01-01 00:00:00', 'otd'),
	(NULL, NULL, NULL, 0, 'DOL.GS.LootGeneratorTemplate', 0, '2000-01-01 00:00:00', 'template');
/*!40000 ALTER TABLE `lootgenerator` ENABLE KEYS */;

