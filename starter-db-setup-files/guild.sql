/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `guild` (
  `GuildID` varchar(255) DEFAULT NULL,
  `GuildName` varchar(255) DEFAULT NULL,
  `Realm` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `GuildBanner` tinyint(1) NOT NULL DEFAULT 0,
  `GuildBannerLostTime` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `Motd` text DEFAULT NULL,
  `oMotd` text DEFAULT NULL,
  `AllianceID` varchar(255) DEFAULT NULL,
  `Emblem` int(11) NOT NULL DEFAULT 0,
  `RealmPoints` bigint(20) NOT NULL DEFAULT 0,
  `BountyPoints` bigint(20) NOT NULL DEFAULT 0,
  `Webpage` text DEFAULT NULL,
  `Email` text DEFAULT NULL,
  `Dues` tinyint(1) NOT NULL DEFAULT 0,
  `Bank` double NOT NULL DEFAULT 0,
  `DuesPercent` bigint(20) NOT NULL DEFAULT 0,
  `HaveGuildHouse` tinyint(1) NOT NULL DEFAULT 0,
  `GuildHouseNumber` int(11) NOT NULL DEFAULT 0,
  `GuildLevel` bigint(20) NOT NULL DEFAULT 0,
  `BonusType` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `BonusStartTime` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `MeritPoints` bigint(20) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `Guild_ID` varchar(255) NOT NULL,
  `IsStartingGuild` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Guild_ID`),
  UNIQUE KEY `U_Guild_GuildID` (`GuildID`),
  KEY `I_Guild_GuildName` (`GuildName`),
  KEY `I_Guild_AllianceID` (`AllianceID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `guild` DISABLE KEYS */;
/*!40000 ALTER TABLE `guild` ENABLE KEYS */;

