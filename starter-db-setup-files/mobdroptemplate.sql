/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `mobdroptemplate` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `MobName` varchar(255) NOT NULL,
  `LootTemplateName` varchar(255) NOT NULL,
  `DropCount` int(11) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  PRIMARY KEY (`ID`),
  KEY `I_MobDropTemplate_MobName` (`MobName`),
  KEY `I_MobDropTemplate_LootTemplateName` (`LootTemplateName`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `mobdroptemplate` DISABLE KEYS */;
/*!40000 ALTER TABLE `mobdroptemplate` ENABLE KEYS */;

