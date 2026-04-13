/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `dbhousepermissions` (
  `PermissionLevel` int(11) NOT NULL DEFAULT 0,
  `HouseNumber` int(11) NOT NULL DEFAULT 0,
  `CanEnterHouse` tinyint(1) NOT NULL DEFAULT 0,
  `Vault1` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `Vault2` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `Vault3` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `Vault4` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `CanChangeExternalAppearance` tinyint(1) NOT NULL DEFAULT 0,
  `ChangeInterior` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `ChangeGarden` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `CanBanish` tinyint(1) NOT NULL DEFAULT 0,
  `CanUseMerchants` tinyint(1) NOT NULL DEFAULT 0,
  `CanUseTools` tinyint(1) NOT NULL DEFAULT 0,
  `CanBindInHouse` tinyint(1) NOT NULL DEFAULT 0,
  `ConsignmentMerchant` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `CanPayRent` tinyint(1) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `DBHousePermissions_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`DBHousePermissions_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `dbhousepermissions` DISABLE KEYS */;
/*!40000 ALTER TABLE `dbhousepermissions` ENABLE KEYS */;

