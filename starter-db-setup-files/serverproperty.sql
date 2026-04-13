/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `serverproperty` (
  `Category` text NOT NULL,
  `Key` varchar(255) NOT NULL,
  `Description` text NOT NULL,
  `DefaultValue` text NOT NULL,
  `Value` text NOT NULL,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `ServerProperty_ID` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`Key`),
  UNIQUE KEY `U_ServerProperty_ServerProperty_ID` (`ServerProperty_ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `serverproperty` DISABLE KEYS */;
/*!40000 ALTER TABLE `serverproperty` ENABLE KEYS */;

