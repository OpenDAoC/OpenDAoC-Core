/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `race` (
  `ID` int(11) NOT NULL DEFAULT 0,
  `Name` varchar(255) NOT NULL,
  `ResistBody` tinyint(3) NOT NULL,
  `ResistCold` tinyint(3) NOT NULL,
  `ResistCrush` tinyint(3) NOT NULL,
  `ResistEnergy` tinyint(3) NOT NULL,
  `ResistHeat` tinyint(3) NOT NULL,
  `ResistMatter` tinyint(3) NOT NULL,
  `ResistNatural` tinyint(3) NOT NULL,
  `ResistSlash` tinyint(3) NOT NULL,
  `ResistSpirit` tinyint(3) NOT NULL,
  `ResistThrust` tinyint(3) NOT NULL,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `Race_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`Race_ID`),
  UNIQUE KEY `U_Race_ID` (`ID`),
  UNIQUE KEY `U_Race_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `race` DISABLE KEYS */;
REPLACE INTO `race` (`ID`, `Name`, `ResistBody`, `ResistCold`, `ResistCrush`, `ResistEnergy`, `ResistHeat`, `ResistMatter`, `ResistNatural`, `ResistSlash`, `ResistSpirit`, `ResistThrust`, `LastTimeRowUpdated`, `Race_ID`) VALUES
	(1, 'Briton', 0, 0, 2, 0, 0, 0, 0, 3, 5, 0, '2000-01-01 00:00:00', '1'),
	(10, 'Firbolg', 0, 0, 3, 0, 5, 0, 0, 2, 0, 0, '2000-01-01 00:00:00', '10'),
	(11, 'Elf', 0, 0, 0, 0, 0, 0, 0, 2, 5, 3, '2000-01-01 00:00:00', '11'),
	(12, 'Lurikeen', 0, 0, 5, 5, 0, 0, 0, 0, 0, 0, '2000-01-01 00:00:00', '12'),
	(13, 'Inconnu', 0, 0, 2, 0, 5, 0, 0, 0, 5, 3, '2000-01-01 00:00:00', '13'),
	(14, 'Valkyn', 5, 5, 0, 0, 0, 0, 0, 3, 0, 2, '2000-01-01 00:00:00', '14'),
	(15, 'Sylvan', 0, 0, 3, 5, 0, 5, 0, 0, 0, 2, '2000-01-01 00:00:00', '15'),
	(16, 'HalfOgre', 0, 0, 0, 0, 0, 5, 0, 3, 0, 2, '2000-01-01 00:00:00', '16'),
	(17, 'Frostalf', 0, 0, 0, 0, 0, 0, 0, 2, 5, 3, '2000-01-01 00:00:00', '17'),
	(18, 'Shar', 0, 0, 5, 5, 0, 0, 0, 0, 0, 0, '2000-01-01 00:00:00', '18'),
	(19, 'AlbionMinotaur', 0, 3, 4, 0, 3, 0, 0, 0, 0, 0, '2000-01-01 00:00:00', '19'),
	(2, 'Avalonian', 0, 0, 2, 0, 0, 0, 0, 3, 5, 0, '2000-01-01 00:00:00', '2'),
	(20, 'MidgardMinotaur', 0, 3, 4, 0, 3, 0, 0, 0, 0, 0, '2000-01-01 00:00:00', '20'),
	(21, 'HiberniaMinotaur', 0, 3, 4, 0, 3, 0, 0, 0, 0, 0, '2000-01-01 00:00:00', '21'),
	(3, 'Highlander', 0, 5, 3, 0, 0, 0, 0, 2, 0, 0, '2000-01-01 00:00:00', '3'),
	(4, 'Saracen', 0, 0, 0, 0, 5, 0, 0, 2, 0, 3, '2000-01-01 00:00:00', '4'),
	(5, 'Norseman', 0, 5, 2, 0, 0, 0, 0, 3, 0, 0, '2000-01-01 00:00:00', '5'),
	(6, 'Troll', 0, 0, 0, 0, 0, 5, 0, 3, 0, 2, '2000-01-01 00:00:00', '6'),
	(7, 'Dwarf', 5, 0, 0, 0, 0, 0, 0, 2, 0, 3, '2000-01-01 00:00:00', '7'),
	(8, 'Kobold', 0, 0, 5, 5, 0, 0, 0, 0, 0, 0, '2000-01-01 00:00:00', '8'),
	(9, 'Celt', 0, 0, 2, 0, 0, 0, 0, 3, 5, 0, '2000-01-01 00:00:00', '9'),
	(50, 'Ancients', 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, '2000-01-01 00:00:00', 'Ancients'),
	(2000, 'Animal', 0, -15, 0, 0, 0, 0, 0, 5, 0, -15, '2000-01-01 00:00:00', 'Animal'),
	(2001, 'Demon', 5, 10, 15, 0, 15, 10, 5, 0, -5, -10, '2000-01-01 00:00:00', 'Demon'),
	(26, 'Dragons', 21, 20, 15, 32, 40, 15, 0, 15, 22, 15, '2000-01-01 00:00:00', 'Dragons'),
	(2002, 'Drake', 10, 15, 0, 15, 10, 5, 0, -5, -10, -15, '2000-01-01 00:00:00', 'Drake'),
	(2003, 'Elemental', 15, 0, 15, 10, 5, 0, -5, -10, -15, 0, '2000-01-01 00:00:00', 'Elemental'),
	(2004, 'Giant', 5, 15, 15, -15, -25, 0, 0, -10, 0, -10, '2000-01-01 00:00:00', 'Giant'),
	(2005, 'Humanoid', 15, 10, 5, 0, -5, -10, -15, 0, -15, -10, '2000-01-01 00:00:00', 'Humanoid'),
	(2010, 'Insect', -10, -15, 0, -15, -10, -5, 0, 5, 10, 15, '2000-01-01 00:00:00', 'Insect'),
	(24, 'Leviathan', 15, 15, 0, 15, 15, 15, 15, 0, 15, 0, '2000-01-01 00:00:00', 'Leviathan'),
	(23, 'Magic', 0, 0, 100, 0, 0, 0, 0, 100, 0, 100, '2000-01-01 00:00:00', 'Magic'),
	(2006, 'Magical', 10, 5, 0, -5, -10, -15, 0, -15, -10, -5, '2000-01-01 00:00:00', 'Magical'),
	(22, 'Might', 100, 100, 0, 100, 100, 100, 100, 0, 100, 0, '2000-01-01 00:00:00', 'Might'),
	(2011, 'Monster', -15, 0, -15, -10, -5, 0, 5, 10, 15, 0, '2000-01-01 00:00:00', 'Monster'),
	(3000, 'Nok', 100, 100, 25, 100, 100, 100, 100, 25, 100, 25, '2000-01-01 00:00:00', 'Nok'),
	(2007, 'Plant', 15, 0, 0, 10, -20, 0, 0, -20, 0, 15, '2000-01-01 00:00:00', 'Plant'),
	(2008, 'Reptile', 0, -15, 0, 0, 0, 0, 0, 15, 0, -15, '2000-01-01 00:00:00', 'Reptile'),
	(2033, 'Unknown', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '2000-01-01 00:00:00', 'tolastub_missingrace2033'),
	(2009, 'Undead', -5, 15, -20, 0, -5, -10, 0, 0, -30, 20, '2000-01-01 00:00:00', 'Undead'),
	(25, 'wintryquestmobs', 22, 35, 25, 20, 14, 18, 50, 26, 23, 24, '2000-01-01 00:00:00', 'wintry');
/*!40000 ALTER TABLE `race` ENABLE KEYS */;

