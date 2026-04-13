/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `househookpointitem` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `HouseNumber` int(11) NOT NULL DEFAULT 0,
  `HookpointID` int(10) unsigned NOT NULL DEFAULT 0,
  `Heading` smallint(5) unsigned NOT NULL DEFAULT 0,
  `ItemTemplateID` text DEFAULT NULL,
  `Index` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  PRIMARY KEY (`ID`),
  KEY `I_househookpointitem_HouseNumber` (`HouseNumber`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `househookpointitem` DISABLE KEYS */;
/*!40000 ALTER TABLE `househookpointitem` ENABLE KEYS */;

