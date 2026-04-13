/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `spellxcustomvalues` (
  `SpellXCustomValuesID` int(11) NOT NULL,
  `SpellID` int(11) NOT NULL,
  `KeyName` varchar(100) NOT NULL,
  `Value` varchar(255) DEFAULT NULL,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `CustomParamID` int(11) NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`CustomParamID`),
  KEY `I_SpellXCustomValues_SpellID` (`SpellID`),
  KEY `I_SpellXCustomValues_KeyName` (`KeyName`)
) ENGINE=InnoDB AUTO_INCREMENT=44 DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `spellxcustomvalues` DISABLE KEYS */;
REPLACE INTO `spellxcustomvalues` (`SpellXCustomValuesID`, `SpellID`, `KeyName`, `Value`, `LastTimeRowUpdated`, `CustomParamID`) VALUES
	(0, 1118, 'InternalIconID', '1374', '2000-01-01 00:00:00', 1),
	(0, 1175, 'InternalIconID', '929', '2000-01-01 00:00:00', 2),
	(0, 1116, 'InternalIconID', '1372', '2000-01-01 00:00:00', 3),
	(0, 5172, 'InternalIconID', '1461', '2000-01-01 00:00:00', 4),
	(0, 1122, 'InternalIconID', '1461', '2000-01-01 00:00:00', 5),
	(0, 4973, 'InternalIconID', '827', '2000-01-01 00:00:00', 6),
	(0, 5162, 'InternalIconID', '1541', '2000-01-01 00:00:00', 7),
	(0, 1171, 'InternalIconID', '925', '2000-01-01 00:00:00', 8),
	(0, 5153, 'InternalIconID', '322', '2000-01-01 00:00:00', 9),
	(0, 5173, 'InternalIconID', '1462', '2000-01-01 00:00:00', 10),
	(0, 1117, 'InternalIconID', '1373', '2000-01-01 00:00:00', 11),
	(0, 1115, 'InternalIconID', '1371', '2000-01-01 00:00:00', 12),
	(0, 1103, 'InternalIconID', '322', '2000-01-01 00:00:00', 13),
	(0, 5174, 'InternalIconID', '1463', '2000-01-01 00:00:00', 14),
	(0, 1105, 'InternalIconID', '324', '2000-01-01 00:00:00', 15),
	(0, 1102, 'InternalIconID', '321', '2000-01-01 00:00:00', 16),
	(0, 1113, 'InternalIconID', '1370', '2000-01-01 00:00:00', 17),
	(0, 1104, 'InternalIconID', '323', '2000-01-01 00:00:00', 18),
	(0, 5175, 'InternalIconID', '1464', '2000-01-01 00:00:00', 19),
	(0, 1125, 'InternalIconID', '1464', '2000-01-01 00:00:00', 20),
	(0, 1172, 'InternalIconID', '926', '2000-01-01 00:00:00', 21),
	(0, 1174, 'InternalIconID', '928', '2000-01-01 00:00:00', 22),
	(0, 5171, 'InternalIconID', '1460', '2000-01-01 00:00:00', 23),
	(0, 4972, 'InternalIconID', '826', '2000-01-01 00:00:00', 24),
	(0, 4974, 'InternalIconID', '828', '2000-01-01 00:00:00', 25),
	(0, 5154, 'InternalIconID', '323', '2000-01-01 00:00:00', 26),
	(0, 1112, 'InternalIconID', '1370', '2000-01-01 00:00:00', 27),
	(0, 5152, 'InternalIconID', '321', '2000-01-01 00:00:00', 28),
	(0, 1123, 'InternalIconID', '1462', '2000-01-01 00:00:00', 29),
	(0, 5155, 'InternalIconID', '324', '2000-01-01 00:00:00', 30),
	(0, 1101, 'InternalIconID', '320', '2000-01-01 00:00:00', 31),
	(0, 1111, 'InternalIconID', '1370', '2000-01-01 00:00:00', 32),
	(0, 1114, 'InternalIconID', '1371', '2000-01-01 00:00:00', 33),
	(0, 1173, 'InternalIconID', '927', '2000-01-01 00:00:00', 34),
	(0, 1121, 'InternalIconID', '1460', '2000-01-01 00:00:00', 35),
	(0, 5163, 'InternalIconID', '1542', '2000-01-01 00:00:00', 36),
	(0, 1124, 'InternalIconID', '1463', '2000-01-01 00:00:00', 37),
	(0, 5151, 'InternalIconID', '320', '2000-01-01 00:00:00', 38),
	(0, 4971, 'InternalIconID', '825', '2000-01-01 00:00:00', 39),
	(0, 5165, 'InternalIconID', '1544', '2000-01-01 00:00:00', 40),
	(0, 4975, 'InternalIconID', '829', '2000-01-01 00:00:00', 41),
	(0, 5161, 'InternalIconID', '1540', '2000-01-01 00:00:00', 42),
	(0, 5164, 'InternalIconID', '1543', '2000-01-01 00:00:00', 43);
/*!40000 ALTER TABLE `spellxcustomvalues` ENABLE KEYS */;

