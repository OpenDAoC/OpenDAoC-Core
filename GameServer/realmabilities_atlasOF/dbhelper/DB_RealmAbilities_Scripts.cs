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
        `AbilityKey` TEXT NOT NULL COLLATE 'utf8',
        `LastTimeRowUpdated` DATETIME NOT NULL DEFAULT '2000-01-01 00:00:00',
        `ClassXRealmAbility_ID` VARCHAR(255) NOT NULL COLLATE 'utf8' DEFAULT '',
        `ClassXRealmAbility_Atlas_ID` VARCHAR(255) NOT NULL COLLATE 'utf8' DEFAULT '',
        PRIMARY KEY (`ClassXRealmAbility_ID`) USING BTREE
        )
        COLLATE='utf8'
        ENGINE=MyISAM
        ;



        -- ---------------------------------------------------------------------------
        -- ADD REALM ABILITES WITH FOREIGN KEY FROM TABLE : ability
        -- ---------------------------------------------------------------------------

        -- ---------------------------------------------------------------------------
        -- ----------------------- REALM ALBION --------------------------------------
        -- ---------------------------------------------------------------------------

        -- ---------------------------------------------------------------------------
        -- PALADIN
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_AugStr', 'Paladin1-0-1','Paladin1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_MasteryOfArms', 'Paladin1-0-2','Paladin1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_AugDex', 'Paladin1-0-3','Paladin1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_MasteryOfPain', 'Paladin1-0-4','Paladin1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_MasteryOfBlocking', 'Paladin1-0-5','Paladin1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_MasteryOfParrying', 'Paladin1-0-6','Paladin1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_HailOfBlows', 'Paladin1-0-7','Paladin1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_AugCon', 'Paladin1-0-8','Paladin1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_AvoidPain', 'Paladin1-0-9','Paladin1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_SecondWind', 'Paladin1-1-1','Paladin1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_ArmorOfFaith', 'Paladin1-1-2','Paladin1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_BattleYell', 'Paladin1-1-3','Paladin1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_AugQui', 'Paladin1-1-4','Paladin1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_AugAcuity', 'Paladin1-1-5','Paladin1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_MasteryOfTheArcane', 'Paladin1-1-6','Paladin1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_LongWind', 'Paladin1-1-7','Paladin1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_Tireless', 'Paladin1-1-8','Paladin1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_Regeneration', 'Paladin1-1-9','Paladin1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_DeterminationHybrid', 'Paladin1-2-0','Paladin1-2-0');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_Toughness', 'Paladin1-2-1','Paladin1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_MasteryOfWater', 'Paladin1-2-2','Paladin1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_AvoidanceOfMagic', 'Paladin1-2-3','Paladin1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_Lifter', 'Paladin1-2-4','Paladin1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_VeilRecovery', 'Paladin1-2-5','Paladin1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_Trip', 'Paladin1-2-6','Paladin1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_Grapple', 'Paladin1-2-7','Paladin1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_FirstAid', 'Paladin1-2-8','Paladin1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_IgnorePain', 'Paladin1-2-9','Paladin1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_RainOfFire', 'Paladin1-3-1','Paladin1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_RainOfIce', 'Paladin1-3-2','Paladin1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_RainOfAnnihilation', 'Paladin1-3-3','Paladin1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_EmptyMind', 'Paladin1-3-4','Paladin1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_Purge', 'Paladin1-3-5','Paladin1-3-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('1', 'AtlasOF_FaithHealing', 'Paladin1-3-6','Paladin1-3-6');


        -- ---------------------------------------------------------------------------
        -- ARMSMAN
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_AugStr', 'Armsman1-0-1','Armsman1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_MasteryOfArms', 'Armsman1-0-2','Armsman1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_AugDex', 'Armsman1-0-3','Armsman1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_MasteryOfPain', 'Armsman1-0-4','Armsman1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_MasteryOfBlocking', 'Armsman1-0-5','Armsman1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_MasteryOfParrying', 'Armsman1-0-6','Armsman1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_HailOfBlows', 'Armsman1-0-7','Armsman1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_AugCon', 'Armsman1-0-8','Armsman1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_AvoidPain', 'Armsman1-0-9','Armsman1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_SecondWind', 'Armsman1-1-1','Armsman1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_BattleYell', 'Armsman1-1-2','Armsman1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_AugQui', 'Armsman1-1-3','Armsman1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_LongWind', 'Armsman1-1-4','Armsman1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_Tireless', 'Armsman1-1-5','Armsman1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_Regeneration', 'Armsman1-1-6','Armsman1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_Toughness', 'Armsman1-1-7','Armsman1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_MasteryOfWater', 'Armsman1-1-8','Armsman1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_AvoidanceOfMagic', 'Armsman1-1-9','Armsman1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_Lifter', 'Armsman1-2-1','Armsman1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_VeilRecovery', 'Armsman1-2-2','Armsman1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_Determination', 'Armsman1-2-3','Armsman1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_Trip', 'Armsman1-2-4','Armsman1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_Grapple', 'Armsman1-2-5','Armsman1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_FirstAid', 'Armsman1-2-6','Armsman1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_IgnorePain', 'Armsman1-2-7','Armsman1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_RainOfFire', 'Armsman1-2-8','Armsman1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_RainOfIce', 'Armsman1-2-9','Armsman1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_RainOfAnnihilation', 'Armsman1-3-1','Armsman1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_EmptyMind', 'Armsman1-3-2','Armsman1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_Purge', 'Armsman1-3-3','Armsman1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_SoldiersBarricade', 'Armsman1-3-4','Armsman1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('2', 'AtlasOF_PreventFlight', 'Armsman1-3-5','Armsman1-3-5');


        -- ---------------------------------------------------------------------------
        -- MERCENARY
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_AugStr', 'Mercenary1-0-1','Mercenary1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_MasteryOfArms', 'Mercenary1-0-2','Mercenary1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_AugDex', 'Mercenary1-0-3','Mercenary1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_MasteryOfPain', 'Mercenary1-0-4','Mercenary1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_MasteryOfParrying', 'Mercenary1-0-5','Mercenary1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_HailOfBlows', 'Mercenary1-0-6','Mercenary1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_DualistsReflexes', 'Mercenary1-0-7','Mercenary1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_WhirlingDervish', 'Mercenary1-0-8','Mercenary1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_AugCon', 'Mercenary1-0-9','Mercenary1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_SecondWind', 'Mercenary1-1-1','Mercenary1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_AugQui', 'Mercenary1-1-2','Mercenary1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Dodger', 'Mercenary1-1-3','Mercenary1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_LongWind', 'Mercenary1-1-4','Mercenary1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Tireless', 'Mercenary1-1-5','Mercenary1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Regeneration', 'Mercenary1-1-6','Mercenary1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Toughness', 'Mercenary1-1-7','Mercenary1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_MasteryOfWater', 'Mercenary1-1-8','Mercenary1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_AvoidanceOfMagic', 'Mercenary1-1-9','Mercenary1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Lifter', 'Mercenary1-2-1','Mercenary1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_VeilRecovery', 'Mercenary1-2-2','Mercenary1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Determination', 'Mercenary1-2-3','Mercenary1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Trip', 'Mercenary1-2-4','Mercenary1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Grapple', 'Mercenary1-2-5','Mercenary1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_FirstAid', 'Mercenary1-2-6','Mercenary1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_IgnorePain', 'Mercenary1-2-7','Mercenary1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_RainOfFire', 'Mercenary1-2-8','Mercenary1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_RainOfIce', 'Mercenary1-2-9','Mercenary1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_RainOfAnnihilation', 'Mercenary1-3-1','Mercenary1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_EmptyMind', 'Mercenary1-3-2','Mercenary1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_Purge', 'Mercenary1-3-3','Mercenary1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_StyleVoid', 'Mercenary1-3-4','Mercenary1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('11', 'AtlasOF_PreventFlight', 'Mercenary1-3-5','Mercenary1-3-5');


        -- ---------------------------------------------------------------------------
        -- REAVER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_AugStr', 'Reaver1-0-1','Reaver1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_MasteryOfArms', 'Reaver1-0-2','Reaver1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_AugDex', 'Reaver1-0-3','Reaver1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_MasteryOfPain', 'Reaver1-0-4','Reaver1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_MasteryOfBlocking', 'Reaver1-0-5','Reaver1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_MasteryOfParrying', 'Reaver1-0-6','Reaver1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_HailOfBlows', 'Reaver1-0-7','Reaver1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_AugCon', 'Reaver1-0-8','Reaver1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_SecondWind', 'Reaver1-0-9','Reaver1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_ArmorOfFaith', 'Reaver1-1-1','Reaver1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_AugQui', 'Reaver1-1-2','Reaver1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_AugAcuity', 'Reaver1-1-3','Reaver1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_Serenity', 'Reaver1-1-4','Reaver1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_WildArcana', 'Reaver1-1-5','Reaver1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_LongWind', 'Reaver1-1-6','Reaver1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_Tireless', 'Reaver1-1-7','Reaver1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_Regeneration', 'Reaver1-1-8','Reaver1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_Toughness', 'Reaver1-1-9','Reaver1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_DeterminationHybrid', 'Reaver1-2-0','Reaver1-2-0');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_MasteryOfWater', 'Reaver1-2-1','Reaver1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_AvoidanceOfMagic', 'Reaver1-2-2','Reaver1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_Lifter', 'Reaver1-2-3','Reaver1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_VeilRecovery', 'Reaver1-2-4','Reaver1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_FirstAid', 'Reaver1-2-5','Reaver1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_IgnorePain', 'Reaver1-2-6','Reaver1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_RainOfFire', 'Reaver1-2-7','Reaver1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_RainOfIce', 'Reaver1-2-8','Reaver1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_RainOfAnnihilation', 'Reaver1-2-9','Reaver1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_EmptyMind', 'Reaver1-3-1','Reaver1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_Purge', 'Reaver1-3-2','Reaver1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('19', 'AtlasOF_UnquenchableThirst', 'Reaver1-3-3','Reaver1-3-3');


        -- ---------------------------------------------------------------------------
        -- CLERIC
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_AugStr', 'Cleric1-0-1','Cleric1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_AugDex', 'Cleric1-0-2','Cleric1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_AugCon', 'Cleric1-0-3','Cleric1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_AvoidPain', 'Cleric1-0-4','Cleric1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_SecondWind', 'Cleric1-0-5','Cleric1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_ArmorOfFaith', 'Cleric1-0-6','Cleric1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_AugQui', 'Cleric1-0-7','Cleric1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_AugAcuity', 'Cleric1-0-8','Cleric1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_Serenity', 'Cleric1-0-9','Cleric1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_EtherealBond', 'Cleric1-1-1','Cleric1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_WildHealing', 'Cleric1-1-2','Cleric1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_MasteryOfTheArt', 'Cleric1-1-3','Cleric1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_MasteryOfHealing', 'Cleric1-1-4','Cleric1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_MasteryOfMagery', 'Cleric1-1-5','Cleric1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_MasteryOfTheArcane', 'Cleric1-1-6','Cleric1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_MasteryOfConcentration', 'Cleric1-1-7','Cleric1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_MajesticWill', 'Cleric1-1-8','Cleric1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_LongWind', 'Cleric1-1-9','Cleric1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_Tireless', 'Cleric1-2-1','Cleric1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_Regeneration', 'Cleric1-2-2','Cleric1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_Toughness', 'Cleric1-2-3','Cleric1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_MasteryOfWater', 'Cleric1-2-4','Cleric1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_AvoidanceOfMagic', 'Cleric1-2-5','Cleric1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_Lifter', 'Cleric1-2-6','Cleric1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_VeilRecovery', 'Cleric1-2-7','Cleric1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_EmptyMind', 'Cleric1-2-8','Cleric1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_MCL', 'Cleric1-2-9','Cleric1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_RagingPower', 'Cleric1-3-1','Cleric1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_Purge', 'Cleric1-3-2','Cleric1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_BunkerOfFaith', 'Cleric1-3-3','Cleric1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('6', 'AtlasOF_BatteryOfLife', 'Cleric1-3-4','Cleric1-3-4');


        -- ---------------------------------------------------------------------------
        -- FRIAR
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_AugStr', 'Friar1-0-1','Friar1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_MasteryOfArms', 'Friar1-0-2','Friar1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_AugDex', 'Friar1-0-3','Friar1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_MasteryOfPain', 'Friar1-0-4','Friar1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_MasteryOfParrying', 'Friar1-0-5','Friar1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_HailOfBlows', 'Friar1-0-6','Friar1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_AugCon', 'Friar1-0-7','Friar1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_SecondWind', 'Friar1-0-8','Friar1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_ArmorOfFaith', 'Friar1-0-9','Friar1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_AugQui', 'Friar1-1-1','Friar1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_Dodger', 'Friar1-1-2','Friar1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_AugAcuity', 'Friar1-1-3','Friar1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_Serenity', 'Friar1-1-4','Friar1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_MasteryOfHealing', 'Friar1-1-5','Friar1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_MasteryOfTheArcane', 'Friar1-1-6','Friar1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_MasteryOfConcentration', 'Friar1-1-7','Friar1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_LongWind', 'Friar1-1-8','Friar1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_Tireless', 'Friar1-1-9','Friar1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_Regeneration', 'Friar1-2-1','Friar1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_Toughness', 'Friar1-2-2','Friar1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_MasteryOfWater', 'Friar1-2-3','Friar1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_AvoidanceOfMagic', 'Friar1-2-4','Friar1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_Lifter', 'Friar1-2-5','Friar1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_VeilRecovery', 'Friar1-2-6','Friar1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_Trip', 'Friar1-2-7','Friar1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_FirstAid', 'Friar1-2-8','Friar1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_IgnorePain', 'Friar1-2-9','Friar1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_EmptyMind', 'Friar1-3-1','Friar1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_MCL', 'Friar1-3-2','Friar1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_RagingPower', 'Friar1-3-3','Friar1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_Purge', 'Friar1-3-4','Friar1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('10', 'AtlasOF_ReflexAttack', 'Friar1-3-5','Friar1-3-5');


        -- ---------------------------------------------------------------------------
        -- INFILTRATOR
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_AugStr', 'Infiltrator1-0-1','Infiltrator1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_MasteryOfArms', 'Infiltrator1-0-2','Infiltrator1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_AugDex', 'Infiltrator1-0-3','Infiltrator1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_MasteryOfPain', 'Infiltrator1-0-4','Infiltrator1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_DualistsReflexes', 'Infiltrator1-0-5','Infiltrator1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_WhirlingDervish', 'Infiltrator1-0-6','Infiltrator1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_Bladedance', 'Infiltrator1-0-7','Infiltrator1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_AugCon', 'Infiltrator1-0-8','Infiltrator1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_SecondWind', 'Infiltrator1-0-9','Infiltrator1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_AugQui', 'Infiltrator1-1-1','Infiltrator1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_Dodger', 'Infiltrator1-1-2','Infiltrator1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_MasteryOfStealth', 'Infiltrator1-1-3','Infiltrator1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_LongWind', 'Infiltrator1-1-4','Infiltrator1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_Tireless', 'Infiltrator1-1-5','Infiltrator1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_Regeneration', 'Infiltrator1-1-6','Infiltrator1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_Toughness', 'Infiltrator1-1-7','Infiltrator1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_MasteryOfWater', 'Infiltrator1-1-8','Infiltrator1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_AvoidanceOfMagic', 'Infiltrator1-1-9','Infiltrator1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_Lifter', 'Infiltrator1-2-1','Infiltrator1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_VeilRecovery', 'Infiltrator1-2-2','Infiltrator1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_SeeHidden', 'Infiltrator1-2-3','Infiltrator1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_FirstAid', 'Infiltrator1-2-4','Infiltrator1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_RainOfFire', 'Infiltrator1-2-5','Infiltrator1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_RainOfIce', 'Infiltrator1-2-6','Infiltrator1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_RainOfAnnihilation', 'Infiltrator1-2-7','Infiltrator1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_EmptyMind', 'Infiltrator1-2-8','Infiltrator1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_Purge', 'Infiltrator1-2-9','Infiltrator1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('9', 'AtlasOF_Vanish', 'Infiltrator1-3-1','Infiltrator1-3-1');


        -- ---------------------------------------------------------------------------
        -- MINSTREL
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_AugStr', 'Minstrel1-0-1','Minstrel1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_MasteryOfArms', 'Minstrel1-0-2','Minstrel1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_AugDex', 'Minstrel1-0-3','Minstrel1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_MasteryOfPain', 'Minstrel1-0-4','Minstrel1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_MasteryOfBlocking', 'Minstrel1-0-5','Minstrel1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_HailOfBlows', 'Minstrel1-0-6','Minstrel1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_AugCon', 'Minstrel1-0-7','Minstrel1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_AvoidPain', 'Minstrel1-0-8','Minstrel1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_SecondWind', 'Minstrel1-0-9','Minstrel1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_AugQui', 'Minstrel1-1-1','Minstrel1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_Dodger', 'Minstrel1-1-2','Minstrel1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_AugAcuity', 'Minstrel1-1-3','Minstrel1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_Serenity', 'Minstrel1-1-4','Minstrel1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_EtherealBond', 'Minstrel1-1-5','Minstrel1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_MasteryOfTheArcane', 'Minstrel1-1-6','Minstrel1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_LongWind', 'Minstrel1-1-7','Minstrel1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_Tireless', 'Minstrel1-1-8','Minstrel1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_Regeneration', 'Minstrel1-1-9','Minstrel1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_Toughness', 'Minstrel1-2-1','Minstrel1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_MasteryOfWater', 'Minstrel1-2-2','Minstrel1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_AvoidanceOfMagic', 'Minstrel1-2-3','Minstrel1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_Lifter', 'Minstrel1-2-4','Minstrel1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_VeilRecovery', 'Minstrel1-2-5','Minstrel1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_FirstAid', 'Minstrel1-2-6','Minstrel1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_IgnorePain', 'Minstrel1-2-7','Minstrel1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_RainOfFire', 'Minstrel1-2-8','Minstrel1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_RainOfIce', 'Minstrel1-2-9','Minstrel1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_RainOfAnnihilation', 'Minstrel1-3-1','Minstrel1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_EmptyMind', 'Minstrel1-3-2','Minstrel1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_MCL', 'Minstrel1-3-3','Minstrel1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_RagingPower', 'Minstrel1-3-4','Minstrel1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_Purge', 'Minstrel1-3-5','Minstrel1-3-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('4', 'AtlasOF_SpeedOfSound', 'Minstrel1-3-6','Minstrel1-3-6');


        -- ---------------------------------------------------------------------------
        -- SCOUT
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_AugStr', 'Scout1-0-1','Scout1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_AugDex', 'Scout1-0-2','Scout1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_MasteryOfPain', 'Scout1-0-3','Scout1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_MasteryOfBlocking', 'Scout1-0-4','Scout1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_HailOfBlows', 'Scout1-0-5','Scout1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_MasteryOfArchery', 'Scout1-0-6','Scout1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_FalconsEye', 'Scout1-0-7','Scout1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_AugCon', 'Scout1-0-8','Scout1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_AugQui', 'Scout1-0-9','Scout1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_Dodger', 'Scout1-1-1','Scout1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_MasteryOfStealth', 'Scout1-1-2','Scout1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_LongWind', 'Scout1-1-3','Scout1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_Tireless', 'Scout1-1-4','Scout1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_Regeneration', 'Scout1-1-5','Scout1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_Toughness', 'Scout1-1-6','Scout1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_MasteryOfWater', 'Scout1-1-7','Scout1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_AvoidanceOfMagic', 'Scout1-1-8','Scout1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_Lifter', 'Scout1-1-9','Scout1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_VeilRecovery', 'Scout1-2-1','Scout1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_ArrowSalvaging', 'Scout1-2-2','Scout1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_FirstAid', 'Scout1-2-3','Scout1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_IgnorePain', 'Scout1-2-4','Scout1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_RainOfFire', 'Scout1-2-5','Scout1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_RainOfIce', 'Scout1-2-6','Scout1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_RainOfAnnihilation', 'Scout1-2-7','Scout1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_Longshot', 'Scout1-2-8','Scout1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_Volley', 'Scout1-2-9','Scout1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_EmptyMind', 'Scout1-3-1','Scout1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_Purge', 'Scout1-3-2','Scout1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('3', 'AtlasOF_TrueSight', 'Scout1-3-3','Scout1-3-3');


        -- ---------------------------------------------------------------------------
        -- CABALIST
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_AugStr', 'Cabalist1-0-1','Cabalist1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_AugDex', 'Cabalist1-0-2','Cabalist1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_AugCon', 'Cabalist1-0-3','Cabalist1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_SecondWind', 'Cabalist1-0-4','Cabalist1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_AugQui', 'Cabalist1-0-5','Cabalist1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_AugAcuity', 'Cabalist1-0-6','Cabalist1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_Serenity', 'Cabalist1-0-7','Cabalist1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_EtherealBond', 'Cabalist1-0-8','Cabalist1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_WildArcana', 'Cabalist1-0-9','Cabalist1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_WildMinion', 'Cabalist1-1-1','Cabalist1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_WildPower', 'Cabalist1-1-2','Cabalist1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_MasteryOfTheArt', 'Cabalist1-1-3','Cabalist1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_MasteryOfMagery', 'Cabalist1-1-4','Cabalist1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_MasteryOfTheArcane', 'Cabalist1-1-5','Cabalist1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_Concentration', 'Cabalist1-1-6','Cabalist1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_MasteryOfConcentration', 'Cabalist1-1-7','Cabalist1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_MajesticWill', 'Cabalist1-1-8','Cabalist1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_LongWind', 'Cabalist1-1-9','Cabalist1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_Tireless', 'Cabalist1-2-1','Cabalist1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_Regeneration', 'Cabalist1-2-2','Cabalist1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_Toughness', 'Cabalist1-2-3','Cabalist1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_MasteryOfWater', 'Cabalist1-2-4','Cabalist1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_AvoidanceOfMagic', 'Cabalist1-2-5','Cabalist1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_Lifter', 'Cabalist1-2-6','Cabalist1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_VeilRecovery', 'Cabalist1-2-7','Cabalist1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_EmptyMind', 'Cabalist1-2-8','Cabalist1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_MCL', 'Cabalist1-2-9','Cabalist1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_RagingPower', 'Cabalist1-3-1','Cabalist1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_Purge', 'Cabalist1-3-2','Cabalist1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('13', 'AtlasOF_Juggernaut', 'Cabalist1-3-3','Cabalist1-3-3');


        -- ---------------------------------------------------------------------------
        -- SORCERER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_AugStr', 'Sorcerer1-0-1','Sorcerer1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_AugDex', 'Sorcerer1-0-2','Sorcerer1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_AugCon', 'Sorcerer1-0-3','Sorcerer1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_SecondWind', 'Sorcerer1-0-4','Sorcerer1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_AugQui', 'Sorcerer1-0-5','Sorcerer1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_AugAcuity', 'Sorcerer1-0-6','Sorcerer1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_Serenity', 'Sorcerer1-0-7','Sorcerer1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_EtherealBond', 'Sorcerer1-0-8','Sorcerer1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_WildArcana', 'Sorcerer1-0-9','Sorcerer1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_WildMinion', 'Sorcerer1-1-1','Sorcerer1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_WildPower', 'Sorcerer1-1-2','Sorcerer1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_MasteryOfTheArt', 'Sorcerer1-1-3','Sorcerer1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_MasteryOfMagery', 'Sorcerer1-1-4','Sorcerer1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_MasteryOfTheArcane', 'Sorcerer1-1-5','Sorcerer1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_Concentration', 'Sorcerer1-1-6','Sorcerer1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_MasteryOfConcentration', 'Sorcerer1-1-7','Sorcerer1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_MajesticWill', 'Sorcerer1-1-8','Sorcerer1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_LongWind', 'Sorcerer1-1-9','Sorcerer1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_Tireless', 'Sorcerer1-2-1','Sorcerer1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_Regeneration', 'Sorcerer1-2-2','Sorcerer1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_Toughness', 'Sorcerer1-2-3','Sorcerer1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_MasteryOfWater', 'Sorcerer1-2-4','Sorcerer1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_AvoidanceOfMagic', 'Sorcerer1-2-5','Sorcerer1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_Lifter', 'Sorcerer1-2-6','Sorcerer1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_VeilRecovery', 'Sorcerer1-2-7','Sorcerer1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_EmptyMind', 'Sorcerer1-2-8','Sorcerer1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_MCL', 'Sorcerer1-2-9','Sorcerer1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_RagingPower', 'Sorcerer1-3-1','Sorcerer1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_Purge', 'Sorcerer1-3-2','Sorcerer1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('8', 'AtlasOF_CorporealDisintegration', 'Sorcerer1-3-3','Sorcerer1-3-3');


        -- ---------------------------------------------------------------------------
        -- THEURGIST
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_AugStr', 'Theurgist1-0-1','Theurgist1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_AugDex', 'Theurgist1-0-2','Theurgist1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_AugCon', 'Theurgist1-0-3','Theurgist1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_SecondWind', 'Theurgist1-0-4','Theurgist1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_AugQui', 'Theurgist1-0-5','Theurgist1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_AugAcuity', 'Theurgist1-0-6','Theurgist1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_Serenity', 'Theurgist1-0-7','Theurgist1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_EtherealBond', 'Theurgist1-0-8','Theurgist1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_WildArcana', 'Theurgist1-0-9','Theurgist1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_WildPower', 'Theurgist1-1-1','Theurgist1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_MasteryOfTheArt', 'Theurgist1-1-2','Theurgist1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_MasteryOfMagery', 'Theurgist1-1-3','Theurgist1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_MasteryOfTheArcane', 'Theurgist1-1-4','Theurgist1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_Concentration', 'Theurgist1-1-5','Theurgist1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_MasteryOfConcentration', 'Theurgist1-1-6','Theurgist1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_MajesticWill', 'Theurgist1-1-7','Theurgist1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_LongWind', 'Theurgist1-1-8','Theurgist1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_Tireless', 'Theurgist1-1-9','Theurgist1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_Regeneration', 'Theurgist1-2-1','Theurgist1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_Toughness', 'Theurgist1-2-2','Theurgist1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_MasteryOfWater', 'Theurgist1-2-3','Theurgist1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_AvoidanceOfMagic', 'Theurgist1-2-4','Theurgist1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_Lifter', 'Theurgist1-2-5','Theurgist1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_VeilRecovery', 'Theurgist1-2-6','Theurgist1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_FirstAid', 'Theurgist1-2-7','Theurgist1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_EmptyMind', 'Theurgist1-2-8','Theurgist1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_MCL', 'Theurgist1-2-9','Theurgist1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_RagingPower', 'Theurgist1-3-1','Theurgist1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_Purge', 'Theurgist1-3-2','Theurgist1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('5', 'AtlasOF_SiegeBolt', 'Theurgist1-3-3','Theurgist1-3-3');


        -- ---------------------------------------------------------------------------
        -- WIZARD
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_AugStr', 'Wizard1-0-1','Wizard1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_AugDex', 'Wizard1-0-2','Wizard1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_AugCon', 'Wizard1-0-3','Wizard1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_SecondWind', 'Wizard1-0-4','Wizard1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_AugQui', 'Wizard1-0-5','Wizard1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_AugAcuity', 'Wizard1-0-6','Wizard1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_Serenity', 'Wizard1-0-7','Wizard1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_EtherealBond', 'Wizard1-0-8','Wizard1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_WildArcana', 'Wizard1-0-9','Wizard1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_WildPower', 'Wizard1-1-1','Wizard1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_MasteryOfTheArt', 'Wizard1-1-2','Wizard1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_MasteryOfMagery', 'Wizard1-1-3','Wizard1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_MasteryOfTheArcane', 'Wizard1-1-4','Wizard1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_Concentration', 'Wizard1-1-5','Wizard1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_MasteryOfConcentration', 'Wizard1-1-6','Wizard1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_MajesticWill', 'Wizard1-1-7','Wizard1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_LongWind', 'Wizard1-1-8','Wizard1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_Tireless', 'Wizard1-1-9','Wizard1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_Regeneration', 'Wizard1-2-1','Wizard1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_Toughness', 'Wizard1-2-2','Wizard1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_MasteryOfWater', 'Wizard1-2-3','Wizard1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_AvoidanceOfMagic', 'Wizard1-2-4','Wizard1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_Lifter', 'Wizard1-2-5','Wizard1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_VeilRecovery', 'Wizard1-2-6','Wizard1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_FirstAid', 'Wizard1-2-7','Wizard1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_EmptyMind', 'Wizard1-2-8','Wizard1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_MCL', 'Wizard1-2-9','Wizard1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_RagingPower', 'Wizard1-3-1','Wizard1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_Purge', 'Wizard1-3-2','Wizard1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('7', 'AtlasOF_VolcanicPillar', 'Wizard1-3-3','Wizard1-3-3');


        -- ---------------------------------------------------------------------------
        -- NECROMANCER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_AugStr', 'Necromancer1-0-1','Necromancer1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_AugDex', 'Necromancer1-0-2','Necromancer1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_AugCon', 'Necromancer1-0-3','Necromancer1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_SecondWind', 'Necromancer1-0-4','Necromancer1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_AugQui', 'Necromancer1-0-5','Necromancer1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_AugAcuity', 'Necromancer1-0-6','Necromancer1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_Serenity', 'Necromancer1-0-7','Necromancer1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_EtherealBond', 'Necromancer1-0-8','Necromancer1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_WildArcana', 'Necromancer1-0-9','Necromancer1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_WildMinion', 'Necromancer1-1-1','Necromancer1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_WildPower', 'Necromancer1-1-2','Necromancer1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_MasteryOfTheArt', 'Necromancer1-1-3','Necromancer1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_MasteryOfMagery', 'Necromancer1-1-4','Necromancer1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_MasteryOfTheArcane', 'Necromancer1-1-5','Necromancer1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_Concentration', 'Necromancer1-1-6','Necromancer1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_MasteryOfConcentration', 'Necromancer1-1-7','Necromancer1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_MajesticWill', 'Necromancer1-1-8','Necromancer1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_LongWind', 'Necromancer1-1-9','Necromancer1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_Tireless', 'Necromancer1-2-1','Necromancer1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_Regeneration', 'Necromancer1-2-2','Necromancer1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_Toughness', 'Necromancer1-2-3','Necromancer1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_MasteryOfWater', 'Necromancer1-2-4','Necromancer1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_AvoidanceOfMagic', 'Necromancer1-2-5','Necromancer1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_Lifter', 'Necromancer1-2-6','Necromancer1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_VeilRecovery', 'Necromancer1-2-7','Necromancer1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_EmptyMind', 'Necromancer1-2-8','Necromancer1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_MCL', 'Necromancer1-2-9','Necromancer1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_RagingPower', 'Necromancer1-3-1','Necromancer1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_Purge', 'Necromancer1-3-2','Necromancer1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('12', 'AtlasOF_StrikeTheSoul', 'Necromancer1-3-3','Necromancer1-3-3');














        -- ---------------------------------------------------------------------------
        -- ---------------------- REALM HIBERNIA -------------------------------------
        -- ---------------------------------------------------------------------------


        -- ---------------------------------------------------------------------------
        -- BARD
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_AugStr', 'Bard1-0-1','Bard1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_MasteryOfArms', 'Bard1-0-2','Bard1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_AugDex', 'Bard1-0-3','Bard1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_MasteryOfPain', 'Bard1-0-4','Bard1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_MasteryOfBlocking', 'Bard1-0-5','Bard1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_AugCon', 'Bard1-0-6','Bard1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_SecondWind', 'Bard1-0-7','Bard1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_ArmorOfFaith', 'Bard1-0-8','Bard1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_AugQui', 'Bard1-0-9','Bard1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_Dodger', 'Bard1-1-1','Bard1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_AugAcuity', 'Bard1-1-2','Bard1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_Serenity', 'Bard1-1-3','Bard1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_EtherealBond', 'Bard1-1-4','Bard1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_WildHealing', 'Bard1-1-5','Bard1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_MasteryOfTheArt', 'Bard1-1-6','Bard1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_MasteryOfHealing', 'Bard1-1-7','Bard1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_MasteryOfTheArcane', 'Bard1-1-8','Bard1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_LongWind', 'Bard1-1-9','Bard1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_Tireless', 'Bard1-2-1','Bard1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_Regeneration', 'Bard1-2-2','Bard1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_Toughness', 'Bard1-2-3','Bard1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_MasteryOfWater', 'Bard1-2-4','Bard1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_AvoidanceOfMagic', 'Bard1-2-5','Bard1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_Lifter', 'Bard1-2-6','Bard1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_VeilRecovery', 'Bard1-2-7','Bard1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_FirstAid', 'Bard1-2-8','Bard1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_IgnorePain', 'Bard1-2-9','Bard1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_EmptyMind', 'Bard1-3-1','Bard1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_MCL', 'Bard1-3-2','Bard1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_RagingPower', 'Bard1-3-3','Bard1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_Purge', 'Bard1-3-4','Bard1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('48', 'AtlasOF_AmelioratingMelodies', 'Bard1-3-5','Bard1-3-5');


        -- ---------------------------------------------------------------------------
        -- DRUID
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_AugStr', 'Druid1-0-1','Druid1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_AugDex', 'Druid1-0-2','Druid1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_MasteryOfBlocking', 'Druid1-0-3','Druid1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_AugCon', 'Druid1-0-4','Druid1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_SecondWind', 'Druid1-0-5','Druid1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_ArmorOfFaith', 'Druid1-0-6','Druid1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_AugQui', 'Druid1-0-7','Druid1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_AugAcuity', 'Druid1-0-8','Druid1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_Serenity', 'Druid1-0-9','Druid1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_EtherealBond', 'Druid1-1-1','Druid1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_WildHealing', 'Druid1-1-2','Druid1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_WildMinion', 'Druid1-1-3','Druid1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_MasteryOfTheArt', 'Druid1-1-4','Druid1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_MasteryOfHealing', 'Druid1-1-5','Druid1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_MasteryOfTheArcane', 'Druid1-1-6','Druid1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_MasteryOfConcentration', 'Druid1-1-7','Druid1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_LongWind', 'Druid1-1-8','Druid1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_Tireless', 'Druid1-1-9','Druid1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_Regeneration', 'Druid1-2-1','Druid1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_Toughness', 'Druid1-2-2','Druid1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_MasteryOfWater', 'Druid1-2-3','Druid1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_AvoidanceOfMagic', 'Druid1-2-4','Druid1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_Lifter', 'Druid1-2-5','Druid1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_VeilRecovery', 'Druid1-2-6','Druid1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_EmptyMind', 'Druid1-2-7','Druid1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_MCL', 'Druid1-2-8','Druid1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_RagingPower', 'Druid1-2-9','Druid1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_Purge', 'Druid1-3-1','Druid1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_GroupPurge', 'Druid1-3-2','Druid1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('47', 'AtlasOF_BatteryOfLife', 'Druid1-3-3','Druid1-3-3');


        -- ---------------------------------------------------------------------------
        -- WARDEN
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_AugStr', 'Warden1-0-1','Warden1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_MasteryOfArms', 'Warden1-0-2','Warden1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_AugDex', 'Warden1-0-3','Warden1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_MasteryOfPain', 'Warden1-0-4','Warden1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_MasteryOfBlocking', 'Warden1-0-5','Warden1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_MasteryOfParrying', 'Warden1-0-6','Warden1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_AugCon', 'Warden1-0-7','Warden1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_SecondWind', 'Warden1-0-8','Warden1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_ArmorOfFaith', 'Warden1-0-9','Warden1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_BattleYell', 'Warden1-1-1','Warden1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_AugQui', 'Warden1-1-2','Warden1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_AugAcuity', 'Warden1-1-3','Warden1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_Serenity', 'Warden1-1-4','Warden1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_EtherealBond', 'Warden1-1-5','Warden1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_WildHealing', 'Warden1-1-6','Warden1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_MasteryOfHealing', 'Warden1-1-7','Warden1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_MasteryOfTheArcane', 'Warden1-1-8','Warden1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_LongWind', 'Warden1-1-9','Warden1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_Tireless', 'Warden1-2-1','Warden1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_Regeneration', 'Warden1-2-2','Warden1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_Toughness', 'Warden1-2-3','Warden1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_MasteryOfWater', 'Warden1-2-4','Warden1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_AvoidanceOfMagic', 'Warden1-2-5','Warden1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_Lifter', 'Warden1-2-6','Warden1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_VeilRecovery', 'Warden1-2-7','Warden1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_FirstAid', 'Warden1-2-8','Warden1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_IgnorePain', 'Warden1-2-9','Warden1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_RainOfFire', 'Warden1-3-1','Warden1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_RainOfIce', 'Warden1-3-2','Warden1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_RainOfAnnihilation', 'Warden1-3-3','Warden1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_EmptyMind', 'Warden1-3-4','Warden1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_MCL', 'Warden1-3-5','Warden1-3-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_RagingPower', 'Warden1-3-6','Warden1-3-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_Purge', 'Warden1-3-7','Warden1-3-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('46', 'AtlasOF_ThornweedField', 'Warden1-3-8','Warden1-3-8');


        -- ---------------------------------------------------------------------------
        -- BLADEMASTER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_AugStr', 'Blademaster1-0-1','Blademaster1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_MasteryOfArms', 'Blademaster1-0-2','Blademaster1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_AugDex', 'Blademaster1-0-3','Blademaster1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_MasteryOfPain', 'Blademaster1-0-4','Blademaster1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_MasteryOfParrying', 'Blademaster1-0-5','Blademaster1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_HailOfBlows', 'Blademaster1-0-6','Blademaster1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_DualistsReflexes', 'Blademaster1-0-7','Blademaster1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_WhirlingDervish', 'Blademaster1-0-8','Blademaster1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Bladedance', 'Blademaster1-0-9','Blademaster1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_AugCon', 'Blademaster1-1-1','Blademaster1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_SecondWind', 'Blademaster1-1-2','Blademaster1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_AugQui', 'Blademaster1-1-3','Blademaster1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Dodger', 'Blademaster1-1-4','Blademaster1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_LongWind', 'Blademaster1-1-5','Blademaster1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Tireless', 'Blademaster1-1-6','Blademaster1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Regeneration', 'Blademaster1-1-7','Blademaster1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Toughness', 'Blademaster1-1-8','Blademaster1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_MasteryOfWater', 'Blademaster1-1-9','Blademaster1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_AvoidanceOfMagic', 'Blademaster1-2-1','Blademaster1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Lifter', 'Blademaster1-2-2','Blademaster1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_VeilRecovery', 'Blademaster1-2-3','Blademaster1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Determination', 'Blademaster1-2-4','Blademaster1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Trip', 'Blademaster1-2-5','Blademaster1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Grapple', 'Blademaster1-2-6','Blademaster1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_FirstAid', 'Blademaster1-2-7','Blademaster1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_IgnorePain', 'Blademaster1-2-8','Blademaster1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_RainOfFire', 'Blademaster1-2-9','Blademaster1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_RainOfIce', 'Blademaster1-3-1','Blademaster1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_RainOfAnnihilation', 'Blademaster1-3-2','Blademaster1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_EmptyMind', 'Blademaster1-3-3','Blademaster1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_Purge', 'Blademaster1-3-4','Blademaster1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_StyleWinterMoon', 'Blademaster1-3-5','Blademaster1-3-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('43', 'AtlasOF_PreventFlight', 'Blademaster1-3-6','Blademaster1-3-6');


        -- ---------------------------------------------------------------------------
        -- HERO
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_AugStr', 'Hero1-0-1','Hero1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_MasteryOfArms', 'Hero1-0-2','Hero1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_AugDex', 'Hero1-0-3','Hero1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_MasteryOfPain', 'Hero1-0-4','Hero1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_MasteryOfBlocking', 'Hero1-0-5','Hero1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_MasteryOfParrying', 'Hero1-0-6','Hero1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_HailOfBlows', 'Hero1-0-7','Hero1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_AugCon', 'Hero1-0-8','Hero1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_AvoidPain', 'Hero1-0-9','Hero1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_SecondWind', 'Hero1-1-1','Hero1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_ArmorOfFaith', 'Hero1-1-2','Hero1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_BattleYell', 'Hero1-1-3','Hero1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_AugQui', 'Hero1-1-4','Hero1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_LongWind', 'Hero1-1-5','Hero1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_Tireless', 'Hero1-1-6','Hero1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_Regeneration', 'Hero1-1-7','Hero1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_Toughness', 'Hero1-1-8','Hero1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_MasteryOfWater', 'Hero1-1-9','Hero1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_AvoidanceOfMagic', 'Hero1-2-1','Hero1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_Lifter', 'Hero1-2-2','Hero1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_VeilRecovery', 'Hero1-2-3','Hero1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_Determination', 'Hero1-2-4','Hero1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_Trip', 'Hero1-2-5','Hero1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_Grapple', 'Hero1-2-6','Hero1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_FirstAid', 'Hero1-2-7','Hero1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_IgnorePain', 'Hero1-2-8','Hero1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_RainOfFire', 'Hero1-2-9','Hero1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_RainOfIce', 'Hero1-3-1','Hero1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_RainOfAnnihilation', 'Hero1-3-2','Hero1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_EmptyMind', 'Hero1-3-3','Hero1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_Purge', 'Hero1-3-4','Hero1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_StyleRazorback', 'Hero1-3-5','Hero1-3-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('44', 'AtlasOF_PreventFlight', 'Hero1-3-6','Hero1-3-6');


        -- ---------------------------------------------------------------------------
        -- CHAMPION
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_AugStr', 'Champion1-0-1','Champion1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_MasteryOfArms', 'Champion1-0-2','Champion1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_AugDex', 'Champion1-0-3','Champion1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_MasteryOfPain', 'Champion1-0-4','Champion1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_MasteryOfBlocking', 'Champion1-0-5','Champion1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_MasteryOfParrying', 'Champion1-0-6','Champion1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_HailOfBlows', 'Champion1-0-7','Champion1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_AugCon', 'Champion1-0-8','Champion1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_AvoidPain', 'Champion1-0-9','Champion1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_SecondWind', 'Champion1-1-1','Champion1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_AugQui', 'Champion1-1-2','Champion1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_AugAcuity', 'Champion1-1-3','Champion1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_Serenity', 'Champion1-1-4','Champion1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_WildArcana', 'Champion1-1-5','Champion1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_MasteryOfTheArcane', 'Champion1-1-6','Champion1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_LongWind', 'Champion1-1-7','Champion1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_Tireless', 'Champion1-1-8','Champion1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_Regeneration', 'Champion1-1-9','Champion1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_DeterminationHybrid', 'Champion1-2-0','Champion1-2-0');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_Toughness', 'Champion1-2-1','Champion1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_MasteryOfWater', 'Champion1-2-2','Champion1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_AvoidanceOfMagic', 'Champion1-2-3','Champion1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_Lifter', 'Champion1-2-4','Champion1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_VeilRecovery', 'Champion1-2-5','Champion1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_FirstAid', 'Champion1-2-6','Champion1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_IgnorePain', 'Champion1-2-7','Champion1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_RainOfFire', 'Champion1-2-8','Champion1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_RainOfIce', 'Champion1-2-9','Champion1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_RainOfAnnihilation', 'Champion1-3-1','Champion1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_EmptyMind', 'Champion1-3-2','Champion1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_MCL', 'Champion1-3-3','Champion1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_RagingPower', 'Champion1-3-4','Champion1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_Purge', 'Champion1-3-5','Champion1-3-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('45', 'AtlasOF_WrathOfTheChampion', 'Champion1-3-6','Champion1-3-6');

        -- ---------------------------------------------------------------------------
        -- ELDRITCH
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_AugStr', 'Eldritch1-0-1','Eldritch1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_AugDex', 'Eldritch1-0-2','Eldritch1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_AugCon', 'Eldritch1-0-3','Eldritch1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_SecondWind', 'Eldritch1-0-4','Eldritch1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_AugQui', 'Eldritch1-0-5','Eldritch1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_AugAcuity', 'Eldritch1-0-6','Eldritch1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_Serenity', 'Eldritch1-0-7','Eldritch1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_EtherealBond', 'Eldritch1-0-8','Eldritch1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_WildArcana', 'Eldritch1-0-9','Eldritch1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_WildPower', 'Eldritch1-1-1','Eldritch1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_MasteryOfTheArt', 'Eldritch1-1-2','Eldritch1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_MasteryOfMagery', 'Eldritch1-1-3','Eldritch1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_MasteryOfTheArcane', 'Eldritch1-1-4','Eldritch1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_Concentration', 'Eldritch1-1-5','Eldritch1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_MasteryOfConcentration', 'Eldritch1-1-6','Eldritch1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_MajesticWill', 'Eldritch1-1-7','Eldritch1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_LongWind', 'Eldritch1-1-8','Eldritch1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_Tireless', 'Eldritch1-1-9','Eldritch1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_Regeneration', 'Eldritch1-2-1','Eldritch1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_Toughness', 'Eldritch1-2-2','Eldritch1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_MasteryOfWater', 'Eldritch1-2-3','Eldritch1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_AvoidanceOfMagic', 'Eldritch1-2-4','Eldritch1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_Lifter', 'Eldritch1-2-5','Eldritch1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_VeilRecovery', 'Eldritch1-2-6','Eldritch1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_FirstAid', 'Eldritch1-2-7','Eldritch1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_EmptyMind', 'Eldritch1-2-8','Eldritch1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_MCL', 'Eldritch1-2-9','Eldritch1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_RagingPower', 'Eldritch1-3-1','Eldritch1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_Purge', 'Eldritch1-3-2','Eldritch1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('40', 'AtlasOF_NegativeMaelstrom', 'Eldritch1-3-3','Eldritch1-3-3');


        -- ---------------------------------------------------------------------------
        -- ENCHANTER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_AugStr', 'Enchanter1-0-1','Enchanter1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_AugDex', 'Enchanter1-0-2','Enchanter1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_AugCon', 'Enchanter1-0-3','Enchanter1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_SecondWind', 'Enchanter1-0-4','Enchanter1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_AugQui', 'Enchanter1-0-5','Enchanter1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_AugAcuity', 'Enchanter1-0-6','Enchanter1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_Serenity', 'Enchanter1-0-7','Enchanter1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_EtherealBond', 'Enchanter1-0-8','Enchanter1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_WildMinion', 'Enchanter1-0-9','Enchanter1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_WildPower', 'Enchanter1-1-1','Enchanter1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_MasteryOfTheArt', 'Enchanter1-1-2','Enchanter1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_MasteryOfMagery', 'Enchanter1-1-3','Enchanter1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_MasteryOfTheArcane', 'Enchanter1-1-4','Enchanter1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_Concentration', 'Enchanter1-1-5','Enchanter1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_MasteryOfConcentration', 'Enchanter1-1-6','Enchanter1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_MajesticWill', 'Enchanter1-1-7','Enchanter1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_LongWind', 'Enchanter1-1-8','Enchanter1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_Tireless', 'Enchanter1-1-9','Enchanter1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_Regeneration', 'Enchanter1-2-1','Enchanter1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_Toughness', 'Enchanter1-2-2','Enchanter1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_MasteryOfWater', 'Enchanter1-2-3','Enchanter1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_AvoidanceOfMagic', 'Enchanter1-2-4','Enchanter1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_Lifter', 'Enchanter1-2-5','Enchanter1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_VeilRecovery', 'Enchanter1-2-6','Enchanter1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_FirstAid', 'Enchanter1-2-7','Enchanter1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_EmptyMind', 'Enchanter1-2-8','Enchanter1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_MCL', 'Enchanter1-2-9','Enchanter1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_RagingPower', 'Enchanter1-3-1','Enchanter1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_Purge', 'Enchanter1-3-2','Enchanter1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('41', 'AtlasOF_BrilliantAura', 'Enchanter1-3-3','Enchanter1-3-3');


        -- ---------------------------------------------------------------------------
        -- MENTALIST
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_AugStr', 'Mentalist1-0-1','Mentalist1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_AugDex', 'Mentalist1-0-2','Mentalist1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_AugCon', 'Mentalist1-0-3','Mentalist1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_SecondWind', 'Mentalist1-0-4','Mentalist1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_AugQui', 'Mentalist1-0-5','Mentalist1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_AugAcuity', 'Mentalist1-0-6','Mentalist1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_Serenity', 'Mentalist1-0-7','Mentalist1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_EtherealBond', 'Mentalist1-0-8','Mentalist1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_WildArcana', 'Mentalist1-0-9','Mentalist1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_WildHealing', 'Mentalist1-1-1','Mentalist1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_WildPower', 'Mentalist1-1-2','Mentalist1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_MasteryOfTheArt', 'Mentalist1-1-3','Mentalist1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_MasteryOfHealing', 'Mentalist1-1-4','Mentalist1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_MasteryOfMagery', 'Mentalist1-1-5','Mentalist1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_MasteryOfTheArcane', 'Mentalist1-1-6','Mentalist1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_Concentration', 'Mentalist1-1-7','Mentalist1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_MasteryOfConcentration', 'Mentalist1-1-8','Mentalist1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_MajesticWill', 'Mentalist1-1-9','Mentalist1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_LongWind', 'Mentalist1-2-1','Mentalist1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_Tireless', 'Mentalist1-2-2','Mentalist1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_Regeneration', 'Mentalist1-2-3','Mentalist1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_Toughness', 'Mentalist1-2-4','Mentalist1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_MasteryOfWater', 'Mentalist1-2-5','Mentalist1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_AvoidanceOfMagic', 'Mentalist1-2-6','Mentalist1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_Lifter', 'Mentalist1-2-7','Mentalist1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_VeilRecovery', 'Mentalist1-2-8','Mentalist1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_EmptyMind', 'Mentalist1-2-9','Mentalist1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_MCL', 'Mentalist1-3-1','Mentalist1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_RagingPower', 'Mentalist1-3-2','Mentalist1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_Purge', 'Mentalist1-3-3','Mentalist1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('42', 'AtlasOF_SeveringTheTether', 'Mentalist1-3-4','Mentalist1-3-4');


        -- ---------------------------------------------------------------------------
        -- NIGHTSHADE
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_AugStr', 'Nightshade1-0-1','Nightshade1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_AugDex', 'Nightshade1-0-2','Nightshade1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_MasteryOfPain', 'Nightshade1-0-3','Nightshade1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_HailOfBlows', 'Nightshade1-0-4','Nightshade1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_DualistsReflexes', 'Nightshade1-0-5','Nightshade1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_WhirlingDervish', 'Nightshade1-0-6','Nightshade1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_Bladedance', 'Nightshade1-0-7','Nightshade1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_AugCon', 'Nightshade1-0-8','Nightshade1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_AvoidPain', 'Nightshade1-0-9','Nightshade1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_SecondWind', 'Nightshade1-1-1','Nightshade1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_AugQui', 'Nightshade1-1-2','Nightshade1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_Dodger', 'Nightshade1-1-3','Nightshade1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_MasteryOfStealth', 'Nightshade1-1-4','Nightshade1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_LongWind', 'Nightshade1-1-5','Nightshade1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_Tireless', 'Nightshade1-1-6','Nightshade1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_Regeneration', 'Nightshade1-1-7','Nightshade1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_Toughness', 'Nightshade1-1-8','Nightshade1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_MasteryOfWater', 'Nightshade1-1-9','Nightshade1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_AvoidanceOfMagic', 'Nightshade1-2-1','Nightshade1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_Lifter', 'Nightshade1-2-2','Nightshade1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_VeilRecovery', 'Nightshade1-2-3','Nightshade1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_SeeHidden', 'Nightshade1-2-4','Nightshade1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_FirstAid', 'Nightshade1-2-5','Nightshade1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_RainOfFire', 'Nightshade1-2-6','Nightshade1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_RainOfIce', 'Nightshade1-2-7','Nightshade1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_RainOfAnnihilation', 'Nightshade1-2-8','Nightshade1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_EmptyMind', 'Nightshade1-2-9','Nightshade1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_Purge', 'Nightshade1-3-1','Nightshade1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('49', 'AtlasOF_Viper', 'Nightshade1-3-2','Nightshade1-3-2');


        -- ---------------------------------------------------------------------------
        -- RANGER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_AugStr', 'Ranger1-0-1','Ranger1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_MasteryOfArms', 'Ranger1-0-2','Ranger1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_AugDex', 'Ranger1-0-3','Ranger1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_MasteryOfPain', 'Ranger1-0-4','Ranger1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_MasteryOfArchery', 'Ranger1-0-5','Ranger1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_FalconsEye', 'Ranger1-0-6','Ranger1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_WhirlingDervish', 'Ranger1-0-7','Ranger1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Bladedance', 'Ranger1-0-8','Ranger1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_AugCon', 'Ranger1-0-9','Ranger1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_AvoidPain', 'Ranger1-1-1','Ranger1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_SecondWind', 'Ranger1-1-2','Ranger1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_AugQui', 'Ranger1-1-3','Ranger1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Dodger', 'Ranger1-1-4','Ranger1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_AugAcuity', 'Ranger1-1-5','Ranger1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_MasteryOfTheArcane', 'Ranger1-1-6','Ranger1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_LongWind', 'Ranger1-1-7','Ranger1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Tireless', 'Ranger1-1-8','Ranger1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Regeneration', 'Ranger1-1-9','Ranger1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Toughness', 'Ranger1-2-1','Ranger1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_MasteryOfWater', 'Ranger1-2-2','Ranger1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_AvoidanceOfMagic', 'Ranger1-2-3','Ranger1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Lifter', 'Ranger1-2-4','Ranger1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_VeilRecovery', 'Ranger1-2-5','Ranger1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_ArrowSalvaging', 'Ranger1-2-6','Ranger1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_FirstAid', 'Ranger1-2-7','Ranger1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_IgnorePain', 'Ranger1-2-8','Ranger1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Longshot', 'Ranger1-2-9','Ranger1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Volley', 'Ranger1-3-1','Ranger1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_EmptyMind', 'Ranger1-3-2','Ranger1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_Purge', 'Ranger1-3-3','Ranger1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('50', 'AtlasOF_TrueSight', 'Ranger1-3-4','Ranger1-3-4');


        -- ---------------------------------------------------------------------------
        -- ANIMIST
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_AugStr', 'Animist1-0-1','Animist1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_AugDex', 'Animist1-0-2','Animist1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_AugCon', 'Animist1-0-3','Animist1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_SecondWind', 'Animist1-0-4','Animist1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_AugQui', 'Animist1-0-5','Animist1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_AugAcuity', 'Animist1-0-6','Animist1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_Serenity', 'Animist1-0-7','Animist1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_EtherealBond', 'Animist1-0-8','Animist1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_WildArcana', 'Animist1-0-9','Animist1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_WildMinion', 'Animist1-1-1','Animist1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_WildPower', 'Animist1-1-2','Animist1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_MasteryOfTheArt', 'Animist1-1-3','Animist1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_MasteryOfMagery', 'Animist1-1-4','Animist1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_MasteryOfTheArcane', 'Animist1-1-5','Animist1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_Concentration', 'Animist1-1-6','Animist1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_MasteryOfConcentration', 'Animist1-1-7','Animist1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_MajesticWill', 'Animist1-1-8','Animist1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_LongWind', 'Animist1-1-9','Animist1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_Tireless', 'Animist1-2-1','Animist1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_Regeneration', 'Animist1-2-2','Animist1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_Toughness', 'Animist1-2-3','Animist1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_MasteryOfWater', 'Animist1-2-4','Animist1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_AvoidanceOfMagic', 'Animist1-2-5','Animist1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_Lifter', 'Animist1-2-6','Animist1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_VeilRecovery', 'Animist1-2-7','Animist1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_EmptyMind', 'Animist1-2-8','Animist1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_MCL', 'Animist1-2-9','Animist1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_RagingPower', 'Animist1-3-1','Animist1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_Purge', 'Animist1-3-2','Animist1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('55', 'AtlasOF_ForestheartAmbusher', 'Animist1-3-3','Animist1-3-3');


        -- ---------------------------------------------------------------------------
        -- VALEWALKER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_AugStr', 'Valewalker1-0-1','Valewalker1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_MasteryOfArms', 'Valewalker1-0-2','Valewalker1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_AugDex', 'Valewalker1-0-3','Valewalker1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_MasteryOfPain', 'Valewalker1-0-4','Valewalker1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_MasteryOfParrying', 'Valewalker1-0-5','Valewalker1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_HailOfBlows', 'Valewalker1-0-6','Valewalker1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_AugCon', 'Valewalker1-0-7','Valewalker1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_SecondWind', 'Valewalker1-0-8','Valewalker1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_AugQui', 'Valewalker1-0-9','Valewalker1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_Dodger', 'Valewalker1-1-1','Valewalker1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_AugAcuity', 'Valewalker1-1-2','Valewalker1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_Serenity', 'Valewalker1-1-3','Valewalker1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_WildArcana', 'Valewalker1-1-4','Valewalker1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_WildPower', 'Valewalker1-1-5','Valewalker1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_LongWind', 'Valewalker1-1-6','Valewalker1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_Tireless', 'Valewalker1-1-7','Valewalker1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_Regeneration', 'Valewalker1-1-8','Valewalker1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_Toughness', 'Valewalker1-1-9','Valewalker1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_DeterminationHybrid', 'Valewalker1-2-0','Valewalker1-2-0');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_MasteryOfWater', 'Valewalker1-2-1','Valewalker1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_AvoidanceOfMagic', 'Valewalker1-2-2','Valewalker1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_Lifter', 'Valewalker1-2-3','Valewalker1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_VeilRecovery', 'Valewalker1-2-4','Valewalker1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_FirstAid', 'Valewalker1-2-5','Valewalker1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_IgnorePain', 'Valewalker1-2-6','Valewalker1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_RainOfFire', 'Valewalker1-2-7','Valewalker1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_RainOfIce', 'Valewalker1-2-8','Valewalker1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_RainOfAnnihilation', 'Valewalker1-2-9','Valewalker1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_EmptyMind', 'Valewalker1-3-1','Valewalker1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_MCL', 'Valewalker1-3-2','Valewalker1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_RagingPower', 'Valewalker1-3-3','Valewalker1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_Purge', 'Valewalker1-3-4','Valewalker1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('56', 'AtlasOF_DefenderOfTheVale', 'Valewalker1-3-5','Valewalker1-3-5');




        -- ---------------------------------------------------------------------------
        -- ---------------------- REALM MIDGARD --------------------------------------
        -- ---------------------------------------------------------------------------

        -- ---------------------------------------------------------------------------
        -- BERSERKER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_AugStr', 'Berserker1-0-1','Berserker1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_MasteryOfArms', 'Berserker1-0-2','Berserker1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_AugDex', 'Berserker1-0-3','Berserker1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_MasteryOfPain', 'Berserker1-0-4','Berserker1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_MasteryOfParrying', 'Berserker1-0-5','Berserker1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_DualistsReflexes', 'Berserker1-0-6','Berserker1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_WhirlingDervish', 'Berserker1-0-7','Berserker1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Bladedance', 'Berserker1-0-8','Berserker1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_AugCon', 'Berserker1-0-9','Berserker1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_AvoidPain', 'Berserker1-1-1','Berserker1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_SecondWind', 'Berserker1-1-2','Berserker1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_AugQui', 'Berserker1-1-3','Berserker1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Dodger', 'Berserker1-1-4','Berserker1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_LongWind', 'Berserker1-1-5','Berserker1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Tireless', 'Berserker1-1-6','Berserker1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Regeneration', 'Berserker1-1-7','Berserker1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Toughness', 'Berserker1-1-8','Berserker1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_MasteryOfWater', 'Berserker1-1-9','Berserker1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_AvoidanceOfMagic', 'Berserker1-2-1','Berserker1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Lifter', 'Berserker1-2-2','Berserker1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_VeilRecovery', 'Berserker1-2-3','Berserker1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Determination', 'Berserker1-2-4','Berserker1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Trip', 'Berserker1-2-5','Berserker1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Grapple', 'Berserker1-2-6','Berserker1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_FirstAid', 'Berserker1-2-7','Berserker1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_IgnorePain', 'Berserker1-2-8','Berserker1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_RainOfFire', 'Berserker1-2-9','Berserker1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_RainOfIce', 'Berserker1-3-1','Berserker1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_RainOfAnnihilation', 'Berserker1-3-2','Berserker1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_EmptyMind', 'Berserker1-3-3','Berserker1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_Purge', 'Berserker1-3-4','Berserker1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_StyleTundra', 'Berserker1-3-5','Berserker1-3-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('31', 'AtlasOF_PreventFlight', 'Berserker1-3-6','Berserker1-3-6');


        -- ---------------------------------------------------------------------------
        -- WARRIOR
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_AugStr', 'Warrior1-0-1','Warrior1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_MasteryOfArms', 'Warrior1-0-2','Warrior1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_AugDex', 'Warrior1-0-3','Warrior1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_MasteryOfPain', 'Warrior1-0-4','Warrior1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_MasteryOfBlocking', 'Warrior1-0-5','Warrior1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_MasteryOfParrying', 'Warrior1-0-6','Warrior1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_HailOfBlows', 'Warrior1-0-7','Warrior1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_AugCon', 'Warrior1-0-8','Warrior1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_AvoidPain', 'Warrior1-0-9','Warrior1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_SecondWind', 'Warrior1-1-1','Warrior1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_BattleYell', 'Warrior1-1-2','Warrior1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_AugQui', 'Warrior1-1-3','Warrior1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_LongWind', 'Warrior1-1-4','Warrior1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_Tireless', 'Warrior1-1-5','Warrior1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_Regeneration', 'Warrior1-1-6','Warrior1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_Toughness', 'Warrior1-1-7','Warrior1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_MasteryOfWater', 'Warrior1-1-8','Warrior1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_AvoidanceOfMagic', 'Warrior1-1-9','Warrior1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_Lifter', 'Warrior1-2-1','Warrior1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_VeilRecovery', 'Warrior1-2-2','Warrior1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_Determination', 'Warrior1-2-3','Warrior1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_Trip', 'Warrior1-2-4','Warrior1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_Grapple', 'Warrior1-2-5','Warrior1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_FirstAid', 'Warrior1-2-6','Warrior1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_IgnorePain', 'Warrior1-2-7','Warrior1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_RainOfFire', 'Warrior1-2-8','Warrior1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_RainOfIce', 'Warrior1-2-9','Warrior1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_RainOfAnnihilation', 'Warrior1-3-1','Warrior1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_EmptyMind', 'Warrior1-3-2','Warrior1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_Purge', 'Warrior1-3-3','Warrior1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_StyleDoombringer', 'Warrior1-3-4','Warrior1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('22', 'AtlasOF_PreventFlight', 'Warrior1-3-5','Warrior1-3-5');


        -- ---------------------------------------------------------------------------
        -- SKALD
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_AugStr', 'Skald1-0-1','Skald1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_MasteryOfArms', 'Skald1-0-2','Skald1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_AugDex', 'Skald1-0-3','Skald1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_MasteryOfPain', 'Skald1-0-4','Skald1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_MasteryOfBlocking', 'Skald1-0-5','Skald1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_MasteryOfParrying', 'Skald1-0-6','Skald1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_AugCon', 'Skald1-0-7','Skald1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_AvoidPain', 'Skald1-0-8','Skald1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_SecondWind', 'Skald1-0-9','Skald1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_ArmorOfFaith', 'Skald1-1-1','Skald1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_AugQui', 'Skald1-1-2','Skald1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_AugAcuity', 'Skald1-1-3','Skald1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_Serenity', 'Skald1-1-4','Skald1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_MasteryOfTheArcane', 'Skald1-1-5','Skald1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_LongWind', 'Skald1-1-6','Skald1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_Tireless', 'Skald1-1-7','Skald1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_Regeneration', 'Skald1-1-8','Skald1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_Toughness', 'Skald1-1-9','Skald1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_MasteryOfWater', 'Skald1-2-1','Skald1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_AvoidanceOfMagic', 'Skald1-2-2','Skald1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_Lifter', 'Skald1-2-3','Skald1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_VeilRecovery', 'Skald1-2-4','Skald1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_FirstAid', 'Skald1-2-5','Skald1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_IgnorePain', 'Skald1-2-6','Skald1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_RainOfFire', 'Skald1-2-7','Skald1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_RainOfIce', 'Skald1-2-8','Skald1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_RainOfAnnihilation', 'Skald1-2-9','Skald1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_EmptyMind', 'Skald1-3-1','Skald1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_MCL', 'Skald1-3-2','Skald1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_RagingPower', 'Skald1-3-3','Skald1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_Purge', 'Skald1-3-4','Skald1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('24', 'AtlasOF_FuryOfTheGods', 'Skald1-3-5','Skald1-3-5');


        -- ---------------------------------------------------------------------------
        -- THANE
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_AugStr', 'Thane1-0-1','Thane1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_MasteryOfArms', 'Thane1-0-2','Thane1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_AugDex', 'Thane1-0-3','Thane1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_MasteryOfPain', 'Thane1-0-4','Thane1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_MasteryOfBlocking', 'Thane1-0-5','Thane1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_MasteryOfParrying', 'Thane1-0-6','Thane1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_HailOfBlows', 'Thane1-0-7','Thane1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_AugCon', 'Thane1-0-8','Thane1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_SecondWind', 'Thane1-0-9','Thane1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_ArmorOfFaith', 'Thane1-1-1','Thane1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_AugQui', 'Thane1-1-2','Thane1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_AugAcuity', 'Thane1-1-3','Thane1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_Serenity', 'Thane1-1-4','Thane1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_EtherealBond', 'Thane1-1-5','Thane1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_LongWind', 'Thane1-1-6','Thane1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_Tireless', 'Thane1-1-7','Thane1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_Regeneration', 'Thane1-1-8','Thane1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_Toughness', 'Thane1-1-9','Thane1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_DeterminationHybrid', 'Thane1-2-0','Thane1-2-0');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_MasteryOfWater', 'Thane1-2-1','Thane1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_AvoidanceOfMagic', 'Thane1-2-2','Thane1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_Lifter', 'Thane1-2-3','Thane1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_VeilRecovery', 'Thane1-2-4','Thane1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_FirstAid', 'Thane1-2-5','Thane1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_IgnorePain', 'Thane1-2-6','Thane1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_RainOfFire', 'Thane1-2-7','Thane1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_RainOfIce', 'Thane1-2-8','Thane1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_RainOfAnnihilation', 'Thane1-2-9','Thane1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_EmptyMind', 'Thane1-3-1','Thane1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_MCL', 'Thane1-3-2','Thane1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_Purge', 'Thane1-3-3','Thane1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('21', 'AtlasOF_StaticTempest', 'Thane1-3-4','Thane1-3-4');


        -- ---------------------------------------------------------------------------
        -- SAVAGE
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_AugStr', 'Savage1-0-1','Savage1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_MasteryOfArms', 'Savage1-0-2','Savage1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_AugDex', 'Savage1-0-3','Savage1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_MasteryOfPain', 'Savage1-0-4','Savage1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_MasteryOfParrying', 'Savage1-0-5','Savage1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_HailOfBlows', 'Savage1-0-6','Savage1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_DualistsReflexes', 'Savage1-0-7','Savage1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_WhirlingDervish', 'Savage1-0-8','Savage1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Bladedance', 'Savage1-0-9','Savage1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_AugCon', 'Savage1-1-1','Savage1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_SecondWind', 'Savage1-1-2','Savage1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_AugQui', 'Savage1-1-3','Savage1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Dodger', 'Savage1-1-4','Savage1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_LongWind', 'Savage1-1-5','Savage1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Tireless', 'Savage1-1-6','Savage1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Regeneration', 'Savage1-1-7','Savage1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Toughness', 'Savage1-1-8','Savage1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_MasteryOfWater', 'Savage1-1-9','Savage1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_AvoidanceOfMagic', 'Savage1-2-1','Savage1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Lifter', 'Savage1-2-2','Savage1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_VeilRecovery', 'Savage1-2-3','Savage1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Determination', 'Savage1-2-4','Savage1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Trip', 'Savage1-2-5','Savage1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Grapple', 'Savage1-2-6','Savage1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_FirstAid', 'Savage1-2-7','Savage1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_IgnorePain', 'Savage1-2-8','Savage1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_RainOfFire', 'Savage1-2-9','Savage1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_RainOfIce', 'Savage1-3-1','Savage1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_RainOfAnnihilation', 'Savage1-3-2','Savage1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_EmptyMind', 'Savage1-3-3','Savage1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_Purge', 'Savage1-3-4','Savage1-3-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('32', 'AtlasOF_StyleRavager', 'Savage1-3-5','Savage1-3-5');


        -- ---------------------------------------------------------------------------
        -- HEALER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_AugStr', 'Healer1-0-1','Healer1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_AugDex', 'Healer1-0-2','Healer1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_MasteryOfBlocking', 'Healer1-0-3','Healer1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_AugCon', 'Healer1-0-4','Healer1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_SecondWind', 'Healer1-0-5','Healer1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_ArmorOfFaith', 'Healer1-0-6','Healer1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_AugQui', 'Healer1-0-7','Healer1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_AugAcuity', 'Healer1-0-8','Healer1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_Serenity', 'Healer1-0-9','Healer1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_EtherealBond', 'Healer1-1-1','Healer1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_WildArcana', 'Healer1-1-2','Healer1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_WildHealing', 'Healer1-1-3','Healer1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_MasteryOfTheArt', 'Healer1-1-4','Healer1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_MasteryOfHealing', 'Healer1-1-5','Healer1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_MasteryOfTheArcane', 'Healer1-1-6','Healer1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_MasteryOfConcentration', 'Healer1-1-7','Healer1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_LongWind', 'Healer1-1-8','Healer1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_Tireless', 'Healer1-1-9','Healer1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_Regeneration', 'Healer1-2-1','Healer1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_Toughness', 'Healer1-2-2','Healer1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_MasteryOfWater', 'Healer1-2-3','Healer1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_AvoidanceOfMagic', 'Healer1-2-4','Healer1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_Lifter', 'Healer1-2-5','Healer1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_VeilRecovery', 'Healer1-2-6','Healer1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_EmptyMind', 'Healer1-2-7','Healer1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_MCL', 'Healer1-2-8','Healer1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_RagingPower', 'Healer1-2-9','Healer1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_Purge', 'Healer1-3-1','Healer1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_PerfectRecovery', 'Healer1-3-2','Healer1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('26', 'AtlasOF_BatteryOfLife', 'Healer1-3-3','Healer1-3-3');


        -- ---------------------------------------------------------------------------
        -- SHAMAN
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_AugStr', 'Shaman1-0-1','Shaman1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_AugDex', 'Shaman1-0-2','Shaman1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_MasteryOfBlocking', 'Shaman1-0-3','Shaman1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_AugCon', 'Shaman1-0-4','Shaman1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_SecondWind', 'Shaman1-0-5','Shaman1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_ArmorOfFaith', 'Shaman1-0-6','Shaman1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_AugQui', 'Shaman1-0-7','Shaman1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_AugAcuity', 'Shaman1-0-8','Shaman1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_Serenity', 'Shaman1-0-9','Shaman1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_EtherealBond', 'Shaman1-1-1','Shaman1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_WildArcana', 'Shaman1-1-2','Shaman1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_WildHealing', 'Shaman1-1-3','Shaman1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_MasteryOfTheArt', 'Shaman1-1-4','Shaman1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_MasteryOfHealing', 'Shaman1-1-5','Shaman1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_MasteryOfTheArcane', 'Shaman1-1-6','Shaman1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_MasteryOfConcentration', 'Shaman1-1-7','Shaman1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_LongWind', 'Shaman1-1-8','Shaman1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_Tireless', 'Shaman1-1-9','Shaman1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_Regeneration', 'Shaman1-2-1','Shaman1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_Toughness', 'Shaman1-2-2','Shaman1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_MasteryOfWater', 'Shaman1-2-3','Shaman1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_AvoidanceOfMagic', 'Shaman1-2-4','Shaman1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_Lifter', 'Shaman1-2-5','Shaman1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_VeilRecovery', 'Shaman1-2-6','Shaman1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_FirstAid', 'Shaman1-2-7','Shaman1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_IgnorePain', 'Shaman1-2-8','Shaman1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_EmptyMind', 'Shaman1-2-9','Shaman1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_MCL', 'Shaman1-3-1','Shaman1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_RagingPower', 'Shaman1-3-2','Shaman1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_Purge', 'Shaman1-3-3','Shaman1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('28', 'AtlasOF_Ichor', 'Shaman1-3-4','Shaman1-3-4');


        -- ---------------------------------------------------------------------------
        -- HUNTER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_AugStr', 'Hunter1-0-1','Hunter1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_MasteryOfArms', 'Hunter1-0-2','Hunter1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_AugDex', 'Hunter1-0-3','Hunter1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_MasteryOfPain', 'Hunter1-0-4','Hunter1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_HailOfBlows', 'Hunter1-0-5','Hunter1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_MasteryOfArchery', 'Hunter1-0-6','Hunter1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_FalconsEye', 'Hunter1-0-7','Hunter1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_AugCon', 'Hunter1-0-8','Hunter1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_AvoidPain', 'Hunter1-0-9','Hunter1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_SecondWind', 'Hunter1-1-1','Hunter1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_AugQui', 'Hunter1-1-2','Hunter1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_Dodger', 'Hunter1-1-3','Hunter1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_AugAcuity', 'Hunter1-1-4','Hunter1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_WildMinion', 'Hunter1-1-5','Hunter1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_MasteryOfTheArcane', 'Hunter1-1-6','Hunter1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_LongWind', 'Hunter1-1-7','Hunter1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_Tireless', 'Hunter1-1-8','Hunter1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_Regeneration', 'Hunter1-1-9','Hunter1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_Toughness', 'Hunter1-2-1','Hunter1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_MasteryOfWater', 'Hunter1-2-2','Hunter1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_AvoidanceOfMagic', 'Hunter1-2-3','Hunter1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_Lifter', 'Hunter1-2-4','Hunter1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_VeilRecovery', 'Hunter1-2-5','Hunter1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_ArrowSalvaging', 'Hunter1-2-6','Hunter1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_FirstAid', 'Hunter1-2-7','Hunter1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_IgnorePain', 'Hunter1-2-8','Hunter1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_Longshot', 'Hunter1-2-9','Hunter1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_Volley', 'Hunter1-3-1','Hunter1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_EmptyMind', 'Hunter1-3-2','Hunter1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_Purge', 'Hunter1-3-3','Hunter1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('25', 'AtlasOF_TrueSight', 'Hunter1-3-4','Hunter1-3-4');


        -- ---------------------------------------------------------------------------
        -- SHADOWBLADE
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_AugStr', 'Shadowblade1-0-1','Shadowblade1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_MasteryOfArms', 'Shadowblade1-0-2','Shadowblade1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_AugDex', 'Shadowblade1-0-3','Shadowblade1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_MasteryOfPain', 'Shadowblade1-0-4','Shadowblade1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_HailOfBlows', 'Shadowblade1-0-5','Shadowblade1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_DualistsReflexes', 'Shadowblade1-0-6','Shadowblade1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_WhirlingDervish', 'Shadowblade1-0-7','Shadowblade1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_Bladedance', 'Shadowblade1-0-8','Shadowblade1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_AugCon', 'Shadowblade1-0-9','Shadowblade1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_SecondWind', 'Shadowblade1-1-1','Shadowblade1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_AugQui', 'Shadowblade1-1-2','Shadowblade1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_Dodger', 'Shadowblade1-1-3','Shadowblade1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_MasteryOfStealth', 'Shadowblade1-1-4','Shadowblade1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_LongWind', 'Shadowblade1-1-5','Shadowblade1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_Tireless', 'Shadowblade1-1-6','Shadowblade1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_Regeneration', 'Shadowblade1-1-7','Shadowblade1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_Toughness', 'Shadowblade1-1-8','Shadowblade1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_MasteryOfWater', 'Shadowblade1-1-9','Shadowblade1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_AvoidanceOfMagic', 'Shadowblade1-2-1','Shadowblade1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_Lifter', 'Shadowblade1-2-2','Shadowblade1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_VeilRecovery', 'Shadowblade1-2-3','Shadowblade1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_SeeHidden', 'Shadowblade1-2-4','Shadowblade1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_FirstAid', 'Shadowblade1-2-5','Shadowblade1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_RainOfFire', 'Shadowblade1-2-6','Shadowblade1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_RainOfIce', 'Shadowblade1-2-7','Shadowblade1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_RainOfAnnihilation', 'Shadowblade1-2-8','Shadowblade1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_EmptyMind', 'Shadowblade1-2-9','Shadowblade1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_Purge', 'Shadowblade1-3-1','Shadowblade1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('23', 'AtlasOF_ShadowRun', 'Shadowblade1-3-2','Shadowblade1-3-2');


        -- ---------------------------------------------------------------------------
        -- RUNEMASTER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_AugStr', 'Runemaster1-0-1','Runemaster1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_AugDex', 'Runemaster1-0-2','Runemaster1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_AugCon', 'Runemaster1-0-3','Runemaster1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_SecondWind', 'Runemaster1-0-4','Runemaster1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_AugQui', 'Runemaster1-0-5','Runemaster1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_AugAcuity', 'Runemaster1-0-6','Runemaster1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_Serenity', 'Runemaster1-0-7','Runemaster1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_EtherealBond', 'Runemaster1-0-8','Runemaster1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_WildArcana', 'Runemaster1-0-9','Runemaster1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_WildPower', 'Runemaster1-1-1','Runemaster1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_MasteryOfTheArt', 'Runemaster1-1-2','Runemaster1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_MasteryOfMagery', 'Runemaster1-1-3','Runemaster1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_MasteryOfTheArcane', 'Runemaster1-1-4','Runemaster1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_Concentration', 'Runemaster1-1-5','Runemaster1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_MasteryOfConcentration', 'Runemaster1-1-6','Runemaster1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_MajesticWill', 'Runemaster1-1-7','Runemaster1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_LongWind', 'Runemaster1-1-8','Runemaster1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_Tireless', 'Runemaster1-1-9','Runemaster1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_Regeneration', 'Runemaster1-2-1','Runemaster1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_Toughness', 'Runemaster1-2-2','Runemaster1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_MasteryOfWater', 'Runemaster1-2-3','Runemaster1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_AvoidanceOfMagic', 'Runemaster1-2-4','Runemaster1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_Lifter', 'Runemaster1-2-5','Runemaster1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_VeilRecovery', 'Runemaster1-2-6','Runemaster1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_FirstAid', 'Runemaster1-2-7','Runemaster1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_EmptyMind', 'Runemaster1-2-8','Runemaster1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_MCL', 'Runemaster1-2-9','Runemaster1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_RagingPower', 'Runemaster1-3-1','Runemaster1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_Purge', 'Runemaster1-3-2','Runemaster1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('29', 'AtlasOF_RuneOfDecimation', 'Runemaster1-3-3','Runemaster1-3-3');


        -- ---------------------------------------------------------------------------
        -- SPIRITMASTER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_AugStr', 'Spiritmaster1-0-1','Spiritmaster1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_AugDex', 'Spiritmaster1-0-2','Spiritmaster1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_AugCon', 'Spiritmaster1-0-3','Spiritmaster1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_SecondWind', 'Spiritmaster1-0-4','Spiritmaster1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_AugQui', 'Spiritmaster1-0-5','Spiritmaster1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_AugAcuity', 'Spiritmaster1-0-6','Spiritmaster1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_Serenity', 'Spiritmaster1-0-7','Spiritmaster1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_EtherealBond', 'Spiritmaster1-0-8','Spiritmaster1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_WildArcana', 'Spiritmaster1-0-9','Spiritmaster1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_WildMinion', 'Spiritmaster1-1-1','Spiritmaster1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_WildPower', 'Spiritmaster1-1-2','Spiritmaster1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_MasteryOfTheArt', 'Spiritmaster1-1-3','Spiritmaster1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_MasteryOfMagery', 'Spiritmaster1-1-4','Spiritmaster1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_MasteryOfTheArcane', 'Spiritmaster1-1-5','Spiritmaster1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_Concentration', 'Spiritmaster1-1-6','Spiritmaster1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_MasteryOfConcentration', 'Spiritmaster1-1-7','Spiritmaster1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_MajesticWill', 'Spiritmaster1-1-8','Spiritmaster1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_LongWind', 'Spiritmaster1-1-9','Spiritmaster1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_Tireless', 'Spiritmaster1-2-1','Spiritmaster1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_Regeneration', 'Spiritmaster1-2-2','Spiritmaster1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_Toughness', 'Spiritmaster1-2-3','Spiritmaster1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_MasteryOfWater', 'Spiritmaster1-2-4','Spiritmaster1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_AvoidanceOfMagic', 'Spiritmaster1-2-5','Spiritmaster1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_Lifter', 'Spiritmaster1-2-6','Spiritmaster1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_VeilRecovery', 'Spiritmaster1-2-7','Spiritmaster1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_EmptyMind', 'Spiritmaster1-2-8','Spiritmaster1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_MCL', 'Spiritmaster1-2-9','Spiritmaster1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_RagingPower', 'Spiritmaster1-3-1','Spiritmaster1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_Purge', 'Spiritmaster1-3-2','Spiritmaster1-3-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_WhipOfEncouragement', 'Spiritmaster1-3-3','Spiritmaster1-3-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('27', 'AtlasOF_ExcitedFrenzy', 'Spiritmaster1-3-4','Spiritmaster1-3-4');


        -- ---------------------------------------------------------------------------
        -- BONEDANCER
        -- ---------------------------------------------------------------------------
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_AugStr', 'Bonedancer1-0-1','Bonedancer1-0-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_AugDex', 'Bonedancer1-0-2','Bonedancer1-0-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_AugCon', 'Bonedancer1-0-3','Bonedancer1-0-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_SecondWind', 'Bonedancer1-0-4','Bonedancer1-0-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_AugQui', 'Bonedancer1-0-5','Bonedancer1-0-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_AugAcuity', 'Bonedancer1-0-6','Bonedancer1-0-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_Serenity', 'Bonedancer1-0-7','Bonedancer1-0-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_EtherealBond', 'Bonedancer1-0-8','Bonedancer1-0-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_WildArcana', 'Bonedancer1-0-9','Bonedancer1-0-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_WildPower', 'Bonedancer1-1-1','Bonedancer1-1-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_MasteryOfTheArt', 'Bonedancer1-1-2','Bonedancer1-1-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_MasteryOfMagery', 'Bonedancer1-1-3','Bonedancer1-1-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_MasteryOfTheArcane', 'Bonedancer1-1-4','Bonedancer1-1-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_Concentration', 'Bonedancer1-1-5','Bonedancer1-1-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_MasteryOfConcentration', 'Bonedancer1-1-6','Bonedancer1-1-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_MajesticWill', 'Bonedancer1-1-7','Bonedancer1-1-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_LongWind', 'Bonedancer1-1-8','Bonedancer1-1-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_Tireless', 'Bonedancer1-1-9','Bonedancer1-1-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_Regeneration', 'Bonedancer1-2-1','Bonedancer1-2-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_Toughness', 'Bonedancer1-2-2','Bonedancer1-2-2');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_MasteryOfWater', 'Bonedancer1-2-3','Bonedancer1-2-3');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_AvoidanceOfMagic', 'Bonedancer1-2-4','Bonedancer1-2-4');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_Lifter', 'Bonedancer1-2-5','Bonedancer1-2-5');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_VeilRecovery', 'Bonedancer1-2-6','Bonedancer1-2-6');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_EmptyMind', 'Bonedancer1-2-7','Bonedancer1-2-7');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_MCL', 'Bonedancer1-2-8','Bonedancer1-2-8');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_RagingPower', 'Bonedancer1-2-9','Bonedancer1-2-9');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_Purge', 'Bonedancer1-3-1','Bonedancer1-3-1');
        INSERT INTO `atlas`.`classxrealmability_atlas` (`CharClass`, `AbilityKey`, `ClassXRealmAbility_ID`,`ClassXRealmAbility_Atlas_ID`) VALUES ('30', 'AtlasOF_ResilienceOfDeath', 'Bonedancer1-3-2','Bonedancer1-3-2');




        */
    }
}
