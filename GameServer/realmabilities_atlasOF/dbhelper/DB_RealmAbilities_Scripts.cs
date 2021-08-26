using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace realmabilities_atlasOF._temporary_helper
{
    class DB_RealmAbilities_Scripts
    {
        // --------------------------------------------------------------------------------
        // ---------- DATABASE SCRIPT TO RUN FOR NEW ATLAS REALM ABILITIES TABLE ----------
        // ----------               ClassXRealmAbility_Atlas                     ----------
        // --------------------------------------------------------------------------------
        /*

        -- DROP EXISTING TABLE ------------------------------------------------------------
        DROP TABLE `classxrealmability_atlas`;


        -- CREATE NEW EMPTY TABLE ---------------------------------------------------------
        CREATE TABLE `classxrealmability_atlas` (
        `CharClass` INT(11) NOT NULL,
        `AbilityKey` TEXT NOT NULL COLLATE 'latin1_swedish_ci',
        `LastTimeRowUpdated` DATETIME NOT NULL DEFAULT '2000-01-01 00:00:00',
        `ClassXRealmAbility_Atlas_ID` VARCHAR(255) NOT NULL COLLATE 'latin1_swedish_ci',
        PRIMARY KEY (`ClassXRealmAbility_Atlas_ID`) USING BTREE
        )
        COLLATE='latin1_swedish_ci'
        ENGINE=MyISAM
        ;

        -- ADD REALM ABILITES WITH FOREIGN KEY FROM TABLE : ability
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_MCL', 'Minstrel1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_Purge', 'Minstrel1-0-2');

        */
    }
}
