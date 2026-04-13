/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE TABLE IF NOT EXISTS `jumppoint` (
  `Name` varchar(255) NOT NULL,
  `Region` smallint(5) unsigned NOT NULL DEFAULT 0,
  `Xpos` int(11) NOT NULL DEFAULT 0,
  `Ypos` int(11) NOT NULL DEFAULT 0,
  `Zpos` int(11) NOT NULL DEFAULT 0,
  `Heading` smallint(5) unsigned NOT NULL DEFAULT 0,
  `LastTimeRowUpdated` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `JumpPoint_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`JumpPoint_ID`),
  UNIQUE KEY `U_JumpPoint_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;

/*!40000 ALTER TABLE `jumppoint` DISABLE KEYS */;
REPLACE INTO `jumppoint` (`Name`, `Region`, `Xpos`, `Ypos`, `Zpos`, `Heading`, `LastTimeRowUpdated`, `JumpPoint_ID`) VALUES
	('Domnann', 181, 423199, 440356, 5952, 4060, '2021-08-22 23:37:26', '1809c079-ebab-4db1-af4c-bfe184ef1d63'),
	('cale', 250, 33004, 37490, 5866, 3618, '2021-11-30 21:19:21', '266a241c-9b25-4d17-bbf1-30300cee0034'),
	('celestius', 91, 31852, 30077, 15733, 4075, '2021-06-17 15:34:47', '361a81b9-0272-4536-915b-41cf5d4c0903'),
	('GSmiddleofHafheim', 27, 226363, 220414, 5527, 2678, '2021-11-22 11:01:41', '6d405802-4dcf-47a1-8bab-2d05297fc962'),
	('caledonia', 250, 33004, 37490, 5866, 3618, '2021-11-30 21:19:24', '7a0571c8-3f64-4cd9-9729-f391e2ceb2c6'),
	('gsstart', 27, 341857, 383252, 5391, 2102, '2021-11-20 19:37:01', '90b2f0a2-4b8b-49b8-b52f-4018c2ea0bca'),
	('Thid', 252, 32209, 38279, 5031, 3078, '2021-10-07 20:26:01', '935cff0e-3477-4336-979e-8912f48ea683'),
	('SummonersHall', 248, 32059, 40909, 15468, 2040, '2021-11-23 20:26:37', '9a3e7555-cdc2-4ce2-82b7-59fb20ba2657'),
	('stygia', 130, 335718, 454157, 8108, 1312, '2021-06-21 22:27:10', 'afcc196e-42a4-49a4-abc4-0c3922d87ebd'),
	('albevent', 330, 52759, 39528, 4677, 36, '2021-10-02 18:37:18', 'albevent'),
	('Mag_Mell', 200, 347397, 490541, 5200, 3050, '2021-06-16 23:14:59', 'c2d0901d-a06a-4581-ab7e-0613bb6c2792'),
	('cathal2', 165, 52836, 40401, 4672, 441, '2021-10-02 18:37:18', 'cathal2'),
	('Nalliten', 100, 769762, 838067, 4624, 1228, '2021-12-12 00:39:06', 'd877e245-aa44-48c9-a920-8721e2dbcfd8'),
	('jail', 249, 47415, 50223, 25001, 2051, '2021-10-08 23:25:58', 'ff1a58d4-08f5-4241-91f0-2485574838fc'),
	('hibevent', 335, 52836, 40401, 4672, 441, '2021-10-02 18:37:18', 'hibevent'),
	('midevent', 334, 52160, 39862, 5472, 46, '2021-10-02 18:37:18', 'midevent');
/*!40000 ALTER TABLE `jumppoint` ENABLE KEYS */;

