/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `dbhousecharsxperms` (
  `HouseNumber` int(11) NOT NULL DEFAULT 0,
  `PermissionType` int(11) NOT NULL DEFAULT 0,
  `TargetName` text NOT NULL,
  `DisplayName` text NOT NULL,
  `PermissionLevel` int(11) NOT NULL DEFAULT 0,
  `CreationTime` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `DBHouseCharsXPerms_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`DBHouseCharsXPerms_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `dbhousecharsxperms` DISABLE KEYS */;
/*!40000 ALTER TABLE `dbhousecharsxperms` ENABLE KEYS */;

