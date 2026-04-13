/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `account` (
  `Name` varchar(255) NOT NULL,
  `Password` text NOT NULL,
  `CreationDate` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `LastLogin` datetime DEFAULT NULL,
  `Realm` int(11) NOT NULL DEFAULT 0,
  `PrivLevel` int(10) unsigned NOT NULL DEFAULT 0,
  `Status` int(11) NOT NULL DEFAULT 0,
  `Mail` text DEFAULT NULL,
  `LastLoginIP` varchar(255) DEFAULT NULL,
  `LastClientVersion` text DEFAULT NULL,
  `Language` text DEFAULT NULL,
  `IsMuted` tinyint(1) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `Account_ID` varchar(255) DEFAULT NULL,
  `IsWarned` tinyint(1) NOT NULL DEFAULT 0,
  `Notes` text DEFAULT NULL,
  `IsTester` tinyint(1) NOT NULL DEFAULT 0,
  `DiscordID` text DEFAULT NULL,
  `DiscordName` varchar(50) DEFAULT NULL,
  `CharactersTraded` int(11) NOT NULL DEFAULT 0,
  `SoloCharactersTraded` int(11) NOT NULL DEFAULT 0,
  `Realm_Timer_Realm` int(11) NOT NULL DEFAULT 0,
  `Realm_Timer_Last_Combat` datetime DEFAULT NULL,
  `LastDisconnected` datetime DEFAULT NULL,
  PRIMARY KEY (`Name`),
  UNIQUE KEY `U_Account_Account_ID` (`Account_ID`),
  KEY `I_Account_LastLoginIP` (`LastLoginIP`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `account` DISABLE KEYS */;
/*!40000 ALTER TABLE `account` ENABLE KEYS */;

