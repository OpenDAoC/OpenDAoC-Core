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

        -- DROP EXISTING TABLE IF ALREADY EXISTS ------------------------------------------
        DROP TABLE IF EXISTS `classxrealmability_atlas`;


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

        -- ADD REALM ABILITES IN TABLE: ability
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (241, 'AtlasOF_MCL', 'Mystic Crystal Lore', 501, 'Grants a refresh of power based on the percentages listed. Cannot be used when in combat.', 3008, 'DOL.GS.RealmAbilities.AtlasOF_MysticCrystalLoreAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (242, 'AtlasOF_Purge', 'Purge', 502, 'Removes all negative effects but leaves any applicable immunity timers in place.', 3010, 'DOL.GS.RealmAbilities.AtlasOF_PurgeAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (243, 'AtlasOF_AugStr', 'Augmented Strength', 503, 'Increases Strength by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RAStrengthEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (244, 'AtlasOF_AugDex', 'Augmented Dexterity', 504, 'Increases Dexterity by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RADexterityEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (245, 'AtlasOF_AugCon', 'Augmented Constitution', 505, 'Increases Constitution by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RAConstitutionEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (246, 'AtlasOF_AugQui', 'Augmented Quickness', 506, 'Increases Quickness by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RAQuicknessEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (247, 'AtlasOF_AugAcuity', 'Augmented Acuity', 507, 'Increases primary casting stat by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RAAcuityEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (248, 'AtlasOF_LongWind', 'Long Wind', 508, 'Decreases the amount of endurance taken per tick when sprinting, by the number listed.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RALongWind', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (249, 'AtlasOF_MasteryOfPain', 'Mastery of Pain', 509, 'Increases chance to deal a critical hit in melee per listed percentage. (Passes on to Necro Pets)', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfPain', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (250, 'AtlasOF_MasteryOfParrying', 'Mastery of Parrying', 510, 'Increases chance to parry by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfParrying', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (251, 'AtlasOF_MasteryOfBlocking', 'Mastery of Blocking', 511, 'Increases chance to block by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfBlocking', '2000-01-01 00:00:00');

        -- ADD REALM ABILITES WITH FOREIGN KEY FROM TABLE : ability
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_AugAcuity', '2000-01-01 00:00:00', 'Minstrel1-0-1');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_AugCon', '2000-01-01 00:00:00', 'Minstrel1-0-2');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_AugDex', '2000-01-01 00:00:00', 'Minstrel1-0-3');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_AugQui', '2000-01-01 00:00:00', 'Minstrel1-0-4');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_AugStr', '2000-01-01 00:00:00', 'Minstrel1-0-5');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MCL', '2000-01-01 00:00:00', 'Minstrel1-0-6');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_Purge', '2000-01-01 00:00:00', 'Minstrel1-0-7');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_LongWind', '2000-01-01 00:00:00', 'Minstrel1-0-8');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfPain', '2000-01-01 00:00:00', 'Minstrel1-0-9');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfParrying', '2000-01-01 00:00:00', 'Minstrel1-1-0');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfBlocking', '2000-01-01 00:00:00', 'Minstrel1-1-1');

        */
    }
}
