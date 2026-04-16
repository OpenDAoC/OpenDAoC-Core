-- Live DB snapshot before Camelot Hills item model/icon fixes.
-- Captures only itemtemplate rows whose Model values are touched by this fix.

/*M!999999\- enable the sandbox mode */ 
-- MariaDB dump 10.19  Distrib 10.6.25-MariaDB, for debian-linux-gnu (x86_64)
--
-- Host: localhost    Database: opendaoc
-- ------------------------------------------------------
-- Server version	10.6.25-MariaDB-ubu2204

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Dumping data for table `itemtemplate`
--
-- WHERE:  Id_nb IN ('camelot_hills_cracked_bone','camelot_hills_bent_spearhead','camelot_hills_worn_chain_link','camelot_hills_rotted_cloth_scrap','camelot_hills_drone_wing','camelot_hills_stone_chip','camelot_hills_shale_fragment','camelot_hills_sprite_dust','camelot_hills_stolen_copper_trinket','camelot_hills_bandit_sash','camelot_hills_scorched_robe_scrap','camelot_hills_burnt_parchment','camelot_hills_tarnished_holy_symbol','cotswold_cracked_bone','cotswold_rotted_cloth_scrap','cotswold_sprite_dust','cotswold_stolen_copper_trinket')

INSERT INTO `itemtemplate` VALUES ('camelot_hills_bandit_sash',NULL,'bandit sash',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,520,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,22,10,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_bandit_sash',NULL),('camelot_hills_bent_spearhead',NULL,'bent spearhead',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,516,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,24,10,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_bent_spearhead',NULL),('camelot_hills_burnt_parchment',NULL,'burnt parchment',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,520,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,18,10,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_burnt_parchment',NULL),('camelot_hills_cracked_bone',NULL,'cracked bone',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,541,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,6,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_cracked_bone',NULL),('camelot_hills_drone_wing',NULL,'drone wing',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,520,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,8,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_drone_wing',NULL),('camelot_hills_rotted_cloth_scrap',NULL,'rotted cloth scrap',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,520,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,4,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_rotted_cloth_scrap',NULL),('camelot_hills_scorched_robe_scrap',NULL,'scorched robe scrap',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,520,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,16,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_scorched_robe_scrap',NULL),('camelot_hills_shale_fragment',NULL,'shale fragment',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,516,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,55,10,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_shale_fragment',NULL),('camelot_hills_sprite_dust',NULL,'sprite dust',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,497,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,10,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_sprite_dust',NULL),('camelot_hills_stolen_copper_trinket',NULL,'stolen copper trinket',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,517,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,25,10,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_stolen_copper_trinket',NULL),('camelot_hills_stone_chip',NULL,'stone chip',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,516,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,6,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_stone_chip',NULL),('camelot_hills_tarnished_holy_symbol',NULL,'tarnished holy symbol',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,497,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,28,10,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_tarnished_holy_symbol',NULL),('camelot_hills_worn_chain_link',NULL,'worn chain link',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,517,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,16,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 19:47:11','camelot_hills_worn_chain_link',NULL),('cotswold_cracked_bone',NULL,'cracked bone',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,541,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,6,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 17:22:32','cotswold_cracked_bone',NULL),('cotswold_rotted_cloth_scrap',NULL,'rotted cloth scrap',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,520,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,4,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 17:22:32','cotswold_rotted_cloth_scrap',NULL),('cotswold_sprite_dust',NULL,'sprite dust',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,497,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,10,20,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 17:22:32','cotswold_sprite_dust',NULL),('cotswold_stolen_copper_trinket',NULL,'stolen copper trinket',NULL,NULL,0,100,100,0,100,100,100,0,0,0,0,0,40,0,0,0,0,517,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,25,10,0,1,0,'0',0,0,0,NULL,NULL,NULL,0,'2026-04-15 17:22:32','cotswold_stolen_copper_trinket',NULL);
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-15 20:38:17
