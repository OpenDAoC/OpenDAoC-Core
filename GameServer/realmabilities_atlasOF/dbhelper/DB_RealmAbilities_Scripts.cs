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

        -- DEFAULT GAME DATABASE SETTINGS FOR TESTING PURPOSES ----------------------------
        UPDATE `atlas`.`serverproperty` SET `DefaultValue`='True', `Value`='True' WHERE  `Key`='disable_quit_timer';

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
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (267, 'AtlasOF_HailOfBlows', 'Dodger', 527, 'Increases chance to evade by the listed percentage amount.', 0, 'DOL.GS.RealmAbilities.AtlasOF_Dodger', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (266, 'AtlasOF_Dodger', 'Dodger', 526, 'Increases chance to evade by the listed percentage amount.', 0, 'DOL.GS.RealmAbilities.AtlasOF_Dodger', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (265, 'AtlasOF_FalconsEye', 'Falcon\'s Eye', 525, 'Increases the chance of dealing a critical hit with archery by the listed percentage amount.', 0, 'DOL.GS.RealmAbilities.AtlasOF_FalconsEye', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (264, 'AtlasOF_Determination', 'Determination', 524, 'Reduces the duration of all crowd control spells by the listed percentage. Effect is cumulative at each level increase.', 0, 'DOL.GS.RealmAbilities.AtlasOF_DeterminationAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (263, 'AtlasOF_AoM', 'Avoidance of Magic', 523, 'Reduces all magic damage taken by the listed percentage. (This only works on damage. Does not work on disease, dots, or debuffs and does not affect the duration of crowd control spells)', 0, 'DOL.GS.RealmAbilities.AtlasOF_AvoidanceOfMagicAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (262, 'AtlasOF_WildArcana', 'Wild Arcana', 522, 'Increases chance to critical with dots and debuffs by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_WildArcanaAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (261, 'AtlasOF_WildHealing', 'Wild Healing', 521, 'Adds the listed percentage chance to critical heal on each target of a heal spell.', 0, 'DOL.GS.RealmAbilities.AtlasOF_WildHealingAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (260, 'AtlasOF_WildMinion', 'Wild Minion', 520, 'Increases chance of pet dealing a critical hit with melee, archery, or spells, by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_WildMinionAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (259, 'AtlasOF_WildPower', 'Wild Power', 519, 'Increases chance to deal a critical hit with all spells that do damage, including DoTs, by listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_WildPowerAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (258, 'AtlasOF_MasteryOfHealing', 'Mastery of Healing', 518, 'Increases the effectiveness of healing spells by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfHealing', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (257, 'AtlasOF_MasteryOfTheArcane', 'Mastery of the Arcane', 517, 'Increases effectiveness of your buff spells by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfTheArcane', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (256, 'AtlasOF_MasteryOfTheArt', 'Mastery of the Art', 516, 'Increases spellcasting speed by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfTheArt', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (255, 'AtlasOF_MasteryOfWater', 'Mastery of Water', 515, 'Increases swimming speed by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfWater', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (254, 'AtlasOF_MasteryOfArchery', 'Mastery of Archery', 514, 'Increases bow firing speed by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfArchery', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (253, 'AtlasOF_MasteryOfMagery', 'Mastery of Magery', 513, 'Additional effectiveness of magical damage by listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfMagery', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (252, 'AtlasOF_MasteryOfArms', 'Mastery of Arms', 512, 'Increases melee attack speed by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfArms', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (251, 'AtlasOF_MasteryOfBlocking', 'Mastery of Blocking', 511, 'Increases chance to block by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfBlocking', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (250, 'AtlasOF_MasteryOfParrying', 'Mastery of Parrying', 510, 'Increases chance to parry by the listed percentage.', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfParrying', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (249, 'AtlasOF_MasteryOfPain', 'Mastery of Pain', 509, 'Increases chance to deal a critical hit in melee per listed percentage. (Passes on to Necro Pets)', 0, 'DOL.GS.RealmAbilities.AtlasOF_MasteryOfPain', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (248, 'AtlasOF_LongWind', 'Long Wind', 508, 'Decreases the amount of endurance taken per tick when sprinting, by the number listed.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RALongWind', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (247, 'AtlasOF_AugAcuity', 'Augmented Acuity', 507, 'Increases primary casting stat by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RAAcuityEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (246, 'AtlasOF_AugQui', 'Augmented Quickness', 506, 'Increases Quickness by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RAQuicknessEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (245, 'AtlasOF_AugCon', 'Augmented Constitution', 505, 'Increases Constitution by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RAConstitutionEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (244, 'AtlasOF_AugDex', 'Augmented Dexterity', 504, 'Increases Dexterity by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RADexterityEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (243, 'AtlasOF_AugStr', 'Augmented Strength', 503, 'Increases Strength by the listed amount per level.', 0, 'DOL.GS.RealmAbilities.AtlasOF_RAStrengthEnhancer', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (242, 'AtlasOF_Purge', 'Purge', 502, 'Removes all negative effects but leaves any applicable immunity timers in place.', 3010, 'DOL.GS.RealmAbilities.AtlasOF_PurgeAbility', '2000-01-01 00:00:00');
        INSERT INTO `ability` (`AbilityID`, `KeyName`, `Name`, `InternalID`, `Description`, `IconID`, `Implementation`, `LastTimeRowUpdated`) VALUES (241, 'AtlasOF_MCL', 'Mystic Crystal Lore', 501, 'Grants a refresh of power based on the percentages listed. Cannot be used when in combat.', 3008, 'DOL.GS.RealmAbilities.AtlasOF_MysticCrystalLoreAbility', '2000-01-01 00:00:00');


        -- ADD REALM ABILITES WITH FOREIGN KEY FROM TABLE : ability
        -- MINSTREL --
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
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfArms', '2000-01-01 00:00:00', 'Minstrel1-1-2');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfMagery', '2000-01-01 00:00:00', 'Minstrel1-1-3');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfArchery', '2000-01-01 00:00:00', 'Minstrel1-1-4');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfWater', '2000-01-01 00:00:00', 'Minstrel1-1-5');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfTheArt', '2000-01-01 00:00:00', 'Minstrel1-1-6');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfTheArcane', '2000-01-01 00:00:00', 'Minstrel1-1-7');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_MasteryOfHealing', '2000-01-01 00:00:00', 'Minstrel1-1-8');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_AoM', '2000-01-01 00:00:00', 'Minstrel1-1-9');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (4, 'AtlasOF_Dodger', '2000-01-01 00:00:00', 'Minstrel1-2-0');


        -- SORCERER --
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (8, 'AtlasOF_MasteryOfTheArt', '2000-01-01 00:00:00', 'Sorcerer1-0-3');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (8, 'AtlasOF_AugAcuity', '2000-01-01 00:00:00', 'Sorcerer1-0-4');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (8, 'AtlasOF_AugDex', '2000-01-01 00:00:00', 'Sorcerer1-0-5');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (8, 'AtlasOF_MasteryOfMagery', '2000-01-01 00:00:00', 'Sorcerer1-0-6');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (8, 'AtlasOF_WildPower', '2000-01-01 00:00:00', 'Sorcerer1-0-7');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (8, 'AtlasOF_WildMinion', '2000-01-01 00:00:00', 'Sorcerer1-0-8');

        -- CABALIST --
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (13, 'AtlasOF_AugAcuity', '2000-01-01 00:00:00', 'Cabalist1-0-1');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (13, 'AtlasOF_WildMinion', '2000-01-01 00:00:00', 'Cabalist1-0-2');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (13, 'AtlasOF_WildArcana', '2000-01-01 00:00:00', 'Cabalist1-0-3');

        -- FRIAR --
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (10, 'AtlasOF_AugAcuity', '2000-01-01 00:00:00', 'Friar1-0-1');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (10, 'AtlasOF_AugDex', '2000-01-01 00:00:00', 'Friar1-0-2');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (10, 'AtlasOF_WildHealing', '2000-01-01 00:00:00', 'Friar1-0-3');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (10, 'AtlasOF_MasteryOfParrying', '2000-01-01 00:00:00', 'Friar1-1-4');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (10, 'AtlasOF_MasteryOfTheArcane', '2000-01-01 00:00:00', 'Friar1-0-5');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (10, 'AtlasOF_Determination', '2000-01-01 00:00:00', 'Friar1-0-6');

        -- SCOUT --
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (3, 'AtlasOF_AugDex', '2000-01-01 00:00:00', 'Scout1-0-1');
        INSERT INTO `classxrealmability_atlas` (`CharClass`, `AbilityKey`, `LastTimeRowUpdated`, `ClassXRealmAbility_Atlas_ID`) VALUES (3, 'AtlasOF_FalconsEye', '2000-01-01 00:00:00', 'Scout1-0-2');

        */
    }
}
