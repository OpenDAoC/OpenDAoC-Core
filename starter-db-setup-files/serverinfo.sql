/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `serverinfo` (
  `Time` text DEFAULT NULL,
  `ServerName` text DEFAULT NULL,
  `AAC` text DEFAULT NULL,
  `ServerType` text DEFAULT NULL,
  `ServerStatus` text DEFAULT NULL,
  `NumClients` int(11) NOT NULL DEFAULT 0,
  `NumAccounts` int(11) NOT NULL DEFAULT 0,
  `NumMobs` int(11) NOT NULL DEFAULT 0,
  `NumInventoryItems` int(11) NOT NULL DEFAULT 0,
  `NumPlayerChars` int(11) NOT NULL DEFAULT 0,
  `NumMerchantItems` int(11) NOT NULL DEFAULT 0,
  `NumItemTemplates` int(11) NOT NULL DEFAULT 0,
  `NumWorldObjects` int(11) NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `ServerInfo_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`ServerInfo_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `serverinfo` DISABLE KEYS */;
/*!40000 ALTER TABLE `serverinfo` ENABLE KEYS */;

