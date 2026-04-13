/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `guildrank` (
  `GuildID` varchar(255) DEFAULT NULL,
  `Title` text DEFAULT NULL,
  `RankLevel` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `Alli` tinyint(1) NOT NULL DEFAULT 0,
  `Emblem` tinyint(1) NOT NULL DEFAULT 0,
  `Buff` tinyint(1) NOT NULL DEFAULT 0,
  `GcHear` tinyint(1) NOT NULL DEFAULT 0,
  `GcSpeak` tinyint(1) NOT NULL DEFAULT 0,
  `OcHear` tinyint(1) NOT NULL DEFAULT 0,
  `OcSpeak` tinyint(1) NOT NULL DEFAULT 0,
  `AcHear` tinyint(1) NOT NULL DEFAULT 0,
  `AcSpeak` tinyint(1) NOT NULL DEFAULT 0,
  `Invite` tinyint(1) NOT NULL DEFAULT 0,
  `Promote` tinyint(1) NOT NULL DEFAULT 0,
  `Remove` tinyint(1) NOT NULL DEFAULT 0,
  `View` tinyint(1) NOT NULL DEFAULT 0,
  `Claim` tinyint(1) NOT NULL DEFAULT 0,
  `Upgrade` tinyint(1) NOT NULL DEFAULT 0,
  `Release` tinyint(1) NOT NULL DEFAULT 0,
  `Dues` tinyint(1) NOT NULL DEFAULT 0,
  `Withdraw` tinyint(1) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `GuildRank_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`GuildRank_ID`),
  KEY `I_GuildRank_GuildID` (`GuildID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `guildrank` DISABLE KEYS */;
/*!40000 ALTER TABLE `guildrank` ENABLE KEYS */;

