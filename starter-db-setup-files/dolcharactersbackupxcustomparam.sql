/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `dolcharactersbackupxcustomparam` (
  `DOLCharactersObjectId` varchar(255) NOT NULL,
  `KeyName` varchar(100) NOT NULL,
  `Value` varchar(255) DEFAULT NULL,
  `CustomParamID` int(11) NOT NULL AUTO_INCREMENT,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  PRIMARY KEY (`CustomParamID`),
  KEY `I_DOLCharactersBackupXCustomParam_DOLCharactersObjectId` (`DOLCharactersObjectId`),
  KEY `I_DOLCharactersBackupXCustomParam_KeyName` (`KeyName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `dolcharactersbackupxcustomparam` DISABLE KEYS */;
/*!40000 ALTER TABLE `dolcharactersbackupxcustomparam` ENABLE KEYS */;

