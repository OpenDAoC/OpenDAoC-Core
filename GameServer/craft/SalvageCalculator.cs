/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 * 
 * 
 * The following changes to item extensions are needed in the database
 * for this calculator to work
 * 
 * ALBION DATABASE ARMOR CHANGES
Cloth       Extension 0	Quilted	
Cloth2	    Extension 1 Double Stiched	
Leather     Extension 0	Roman
Leather     Extension 1	Cymric
Leather     Extension 2	Siluric
Leather     Extension 3	Padded Leather	
Studded     Extension 0	Studded	
Studded     Extension 1	Boned	
Studded     Extension 2	Lamellar	
Studded     Extension 3	Reinforced Lamellar	
Chain1      Extension 0	Chain ::Chain 2 not in use for Albion::
Chain3      Extension 2	Mail	
Chain4	    Extension 3 Improved Mail	
Plate1	    Extension 0 Plate	
Plate2	    Extension 1 Scaled Plate	
Plate3	    Extension 2 Fluted Plate	
Plate4	    Extension 3 Full Plate	
 * HIBERNIA DATABASE ARMOR CHANGES
Cloth       Extension 0	Woven	
Cloth2      Extension 1	Thick Woven	
Leather1    Extension 0	Brea	
Leather2	Extension 1	Constaic	
Leather3	Extension 2	Cruaigh	
Leather4	Extension 3	Padded Cruaigh	
Reinforced1	Extension 0	Tacuil	
Reinforced2	Extension 1	Nadurtha	
Reinforced3	Extension 2	Cailiocht	
Reinforced4	Extension 3	Heavy Cailiocht	
Scale1	    Extension 0	Cruanach	
Scale2	    Extension 1	Daingean	
Scale3	    Extension 2	Osnadurtha	
Scale4	    Extension 3	Improved Osnadurtha	
Plate2	    Extension 1	Scaled Plate	
Plate3	    Extension 2	Fluted Plate	
Plate4	    Extension 3	Full Plate	
 * MIDGARD DATABASE CHANGES
Cloth	    Extension 0	Padded	
Cloth2	    Extension 1	Thick Padded	
Leather1	Extension 0	Mjuklaedar	
Leather2	Extension 1	Svarlaedar	
Leather3	Extension 2	Starklaedar	
Leather4	Extension 3	Padded Starklaedar	
Studded1	Extension 0	Stelskodd	
Studded2	Extension 1	Svarskodd	
Studded3	Extension 2	Starkaskodd	
Studded4	Extension 3	Heavy Starkaskodd	
Chain1	    Extension 0	Pansarkedja	
Chain2	    Extension 1	Svarkedja	
Chain3	    Extension 2	Starkakedja	
Chain4	    Extension 3	Heavy Starkakedja	
Plate2	    Extension 1	Scaled Plate	
Plate3	    Extension 2	Fluted Plate	
Plate4	    Extension 3	Full Plate	

 * 
Update originsmyisam.itemtemplate set Extension = 1 where Id_nb like "%double_stitched%";
Update originsmyisam.itemtemplate set Extension = 0 where Id_nb like "%double_stitched_quilted_boots";
Update originsmyisam.itemtemplate set Extension = 1 where Id_nb like "%cymric%";
Update originsmyisam.itemtemplate set Extension = 1 where Id_nb like "%boned%";
Update originsmyisam.itemtemplate set Extension = 1 where Id_nb like "%scaled_plate%";
Update originsmyisam.itemtemplate set Extension = 1 where Id_nb like "%thick_woven%";
Update originsmyisam.itemtemplate set Extension = 1 where Id_nb like "%constaic%";
Update originsmyisam.itemtemplate set Extension = 1 where Id_nb like "%nadurtha%";
Update databasename.itemtemplate set Extension = 1 where Id_nb like "%daingean%";
Update databasename.itemtemplate set Extension = 1 where Id_nb like "%thick_padded%";
Update databasename.itemtemplate set Extension = 1 where Id_nb like "%svarlaedar%";
Update databasename.itemtemplate set Extension = 1 where Id_nb like "%svarskodd%";
Update databasename.itemtemplate set Extension = 1 where Id_nb like "%svarkedja%";
Update databasename.itemtemplate set Extension = 1 where Id_nb like "%spiked_circlet%";

Update databasename.itemtemplate set Extension = 2 where Id_nb like "%siluric%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%lamellar%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%mail%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%fluted_plate%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%cruaigh%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%cailiocht%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%osnadurtha%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%starklaedar%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%starkaskodd%";
Update databasename.itemtemplate set Extension = 2 where Id_nb like "%starkakedja%";

Update databasename.itemtemplate set Extension = 3 where Id_nb like "%padded_leather%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%reinforced_lamellar%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%improved_mail%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%full_plate%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%padded_cruaigh%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%heavy_cailiocht%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%improved_osnadurtha%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%padded_starklaedar%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%heavy_starkaskodd%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%heavy_starkakedja%";
Update databasename.itemtemplate set Extension = 3 where Id_nb like "%superior_war_circlet%";

 */


using System.Text.RegularExpressions;

using DOL.Database;

namespace DOL.GS.SalvageCalc
{
    public struct SalvageReturn
    {
        /// <summary>
        /// The ID_nb of the material in the database
        /// </summary>
        public string ID;
        /// <summary>
        /// The name of the material used in text form
        /// </summary>
        public string Material;
        /// <summary>
        /// The maximum material count return to player
        /// </summary>
        public int Count;
        /// <summary>
        /// The level 1 - 9 tier based on item level
        /// </summary>
        public int Tier;
        /// <summary>
        /// The database level of the item
        /// </summary>
        public int Level;
        /// <summary>
        /// The amount of skill required to craft this item
        /// </summary>
        public int Skill;
        /// <summary>
        /// The recomended price the item should be set to
        /// </summary>
        public int MSRP;
    }

    public class SalvageCalculator
    {
        public SalvageReturn GetSalvage(GamePlayer Player, InventoryItem Item)
        {
            SalvageReturn Yield = new SalvageReturn();

            //Set some default return values for the returning fuction
            Yield.ID = string.Empty;
            Yield.Count = 0;

            int ObjectType = Item.Object_Type;
            string Description = Item.Description;
            int ItemType = Item.Item_Type;
            int Extension = (int)Item.SalvageExtension;

            int Difference = 0;

            /*  Use Extension 0 - 3 for crafted armor
             *  Extension 0 = Lower Class
             *  Extension 1 = Middle Class
             *  Extension 2 = Upper Class
             *  Extension 3 = High Class        
            */

            //Determine Item Level
            //For weapons (DPS - 1.2) / .3 = level.
            //For cloth, AF = level. 
            //For other armors, AF/2 = level.

            int MaterialType = 0;
            int SubTier = 0;
            int MaterialSize = 0; //Small, Medium, Large 1, 2, 3


            double iLevel = Item.Level / 5.1;
            Yield.Tier = (int)(Item.Level * 2 / 10);

            //Just incase the level of an item is set greater then 51
            Yield.Tier = Yield.Tier > 9 ? 9 : Yield.Tier;

            #region //Determine the Sub Tier
            switch (Yield.Tier)
            {
                case 0:
                    SubTier = Item.Level;
                    break;
                case 1:
                    SubTier = Item.Level -5;
                    break;
                case 2:
                    SubTier = SubTier = Item.Level - 10;
                    break;
                case 3:
                    SubTier = Item.Level - 15;
                    break;
                case 4:
                    SubTier = Item.Level - 20;
                    break;
                case 5:
                    SubTier = Item.Level - 25;
                    break;
                case 6:
                    SubTier = Item.Level - 30;
                    break;
                case 7:
                    SubTier = Item.Level - 35;
                    break;
                case 8:
                    SubTier = Item.Level - 40;
                    break;
                default:
                    SubTier = Item.Level > 50 ? 51 : Item.Level - 45;
                    break;
            }
#endregion

            eRealm Realm = Player.Realm;

            Yield.Level = Item.Level;
            
            int Material_Offset = 0; //Offset used to select realm
            string[] ClothSalvageList = new string[30];
            #region Cloth Squares
            //Albion
            ClothSalvageList[0] = "woolen_cloth_squares";
            ClothSalvageList[1] = "linen_cloth_squares";
            ClothSalvageList[2] = "brocade_cloth_squares";
            ClothSalvageList[3] = "silk_cloth_squares";
            ClothSalvageList[4] = "gossamer_cloth_squares";
            ClothSalvageList[5] = "sylvan_cloth_squares";
            ClothSalvageList[6] = "seamist_cloth_squares";
            ClothSalvageList[7] = "nightshade_cloth_squares";
            ClothSalvageList[8] = "wyvernskin_cloth_squares";
            ClothSalvageList[9] = "silksteel_cloth_squares";
            //Midgard
            ClothSalvageList[10] = "woolen_cloth_squares";
            ClothSalvageList[11] = "linen_cloth_squares";
            ClothSalvageList[12] = "brocade_cloth_squares";
            ClothSalvageList[13] = "silk_cloth_squares";
            ClothSalvageList[14] = "gossamer_cloth_squares";
            ClothSalvageList[15] = "sylvan_cloth_squares";
            ClothSalvageList[16] = "seamist_cloth_squares";
            ClothSalvageList[17] = "nightshade_cloth_squares";
            ClothSalvageList[18] = "wyvernskin_cloth_squares";
            ClothSalvageList[19] = "silksteel_cloth_squares";
            //Hibernia
            ClothSalvageList[20] = "woolen_cloth_squares";
            ClothSalvageList[21] = "linen_cloth_squares";
            ClothSalvageList[22] = "brocade_cloth_squares";
            ClothSalvageList[23] = "silk_cloth_squares";
            ClothSalvageList[24] = "gossamer_cloth_squares";
            ClothSalvageList[25] = "sylvan_cloth_squares";
            ClothSalvageList[26] = "seamist_cloth_squares";
            ClothSalvageList[27] = "nightshade_cloth_squares";
            ClothSalvageList[28] = "wyvernskin_cloth_squares";
            ClothSalvageList[29] = "silksteel_cloth_squares";
            #endregion
            string[] LeatherSalvageList = new string[30];
            #region Leather Squares
            //Albion
            LeatherSalvageList[0] = "rawhide_leather_squares";
            LeatherSalvageList[1] = "tanned_leather_squares";
            LeatherSalvageList[2] = "cured_leather_squares";
            LeatherSalvageList[3] = "hard_leather_squares";
            LeatherSalvageList[4] = "rigid_leather_squares";
            LeatherSalvageList[5] = "embossed_leather_squares";
            LeatherSalvageList[6] = "imbued_leather_squares";
            LeatherSalvageList[7] = "runed_leather_squares";
            LeatherSalvageList[8] = "eldritch_leather_squares";
            LeatherSalvageList[9] = "tempered_leather_squares";
            //Midgard
            LeatherSalvageList[10] = "rawhide_leather_squares";
            LeatherSalvageList[11] = "tanned_leather_squares";
            LeatherSalvageList[12] = "cured_leather_squares";
            LeatherSalvageList[13] = "hard_leather_squares";
            LeatherSalvageList[14] = "rigid_leather_squares";
            LeatherSalvageList[15] = "embossed_leather_squares";
            LeatherSalvageList[16] = "imbued_leather_squares";
            LeatherSalvageList[17] = "runed_leather_squares";
            LeatherSalvageList[18] = "eldritch_leather_squares";
            LeatherSalvageList[19] = "tempered_leather_squares";
            //Hibernia
            LeatherSalvageList[20] = "rawhide_leather_squares";
            LeatherSalvageList[21] = "tanned_leather_squares";
            LeatherSalvageList[22] = "cured_leather_squares";
            LeatherSalvageList[23] = "hard_leather_squares";
            LeatherSalvageList[24] = "rigid_leather_squares";
            LeatherSalvageList[25] = "embossed_leather_squares";
            LeatherSalvageList[26] = "imbued_leather_squares";
            LeatherSalvageList[27] = "runed_leather_squares";
            LeatherSalvageList[28] = "eldritch_leather_squares";
            LeatherSalvageList[29] = "tempered_leather_squares";
            #endregion
            string[] StripsSalvageList = new string[30];
            #region Strips Metal Combo
            //Albion
            StripsSalvageList[0] = "bronze_metal_bars";
            StripsSalvageList[1] = "iron_metal_bars";
            StripsSalvageList[2] = "steel_metal_bars";
            StripsSalvageList[3] = "alloy_metal_bars";
            StripsSalvageList[4] = "fine_alloy_metal_bars";
            StripsSalvageList[5] = "mithril_metal_bars";
            StripsSalvageList[6] = "adamantium_metal_bars";
            StripsSalvageList[7] = "asterite_metal_bars";
            StripsSalvageList[8] = "netherium_metal_bars";
            StripsSalvageList[9] = "arcanium_metal_bars";
            //Midgard
            StripsSalvageList[10] = "bronze_metal_bars";
            StripsSalvageList[11] = "iron_metal_bars";
            StripsSalvageList[12] = "steel_metal_bars";
            StripsSalvageList[13] = "alloy_metal_bars";
            StripsSalvageList[14] = "fine_alloy_metal_bars";
            StripsSalvageList[15] = "mithril_metal_bars";
            StripsSalvageList[16] = "adamantium_metal_bars";
            StripsSalvageList[17] = "asterite_metal_bars";
            StripsSalvageList[18] = "netherium_metal_bars";
            StripsSalvageList[19] = "arcanium_metal_bars";
            //Hibernia
            StripsSalvageList[20] = "leaf_strips";
            StripsSalvageList[21] = "bone_strips";
            StripsSalvageList[22] = "vine_strips";
            StripsSalvageList[23] = "shell_strips";
            StripsSalvageList[24] = "fossil_strips";
            StripsSalvageList[25] = "amber_strips";
            StripsSalvageList[26] = "coral_strips";
            StripsSalvageList[27] = "chitin_strips";
            StripsSalvageList[28] = "petrified_strips";
            StripsSalvageList[29] = "crystalized_strips";

            #endregion
            string[] WoodSalvageList = new string[30];
            #region Wooden Boards
            //Albion
            WoodSalvageList[0] = "rowan_wooden_boards";
            WoodSalvageList[1] = "elm_wooden_boards";
            WoodSalvageList[2] = "oaken_wooden_boards";
            WoodSalvageList[3] = "ironwood_wooden_boards";
            WoodSalvageList[4] = "heartwood_wooden_boards";
            WoodSalvageList[5] = "runewood_wooden_boards";
            WoodSalvageList[6] = "stonewood_wooden_boards";
            WoodSalvageList[7] = "ebonwood_wooden_boards";
            WoodSalvageList[8] = "dyrwood_wooden_boards";
            WoodSalvageList[9] = "duskwood_wooden_boards";
            //Midgard
            WoodSalvageList[10] = "rowan_wooden_boards";
            WoodSalvageList[11] = "elm_wooden_boards";
            WoodSalvageList[12] = "oaken_wooden_boards";
            WoodSalvageList[13] = "ironwood_wooden_boards";
            WoodSalvageList[14] = "heartwood_wooden_boards";
            WoodSalvageList[15] = "runewood_wooden_boards";
            WoodSalvageList[16] = "stonewood_wooden_boards";
            WoodSalvageList[17] = "ebonwood_wooden_boards";
            WoodSalvageList[18] = "dyrwood_wooden_boards";
            WoodSalvageList[19] = "duskwood_wooden_boards";
            //Hibernia
            WoodSalvageList[20] = "rowan_wooden_boards";
            WoodSalvageList[21] = "elm_wooden_boards";
            WoodSalvageList[22] = "oaken_wooden_boards";
            WoodSalvageList[23] = "ironwood_wooden_boards";
            WoodSalvageList[24] = "heartwood_wooden_boards";
            WoodSalvageList[25] = "runewood_wooden_boards";
            WoodSalvageList[26] = "stonewood_wooden_boards";
            WoodSalvageList[27] = "ebonwood_wooden_boards";
            WoodSalvageList[28] = "dyrwood_wooden_boards";
            WoodSalvageList[29] = "duskwood_wooden_boards";
            #endregion
            string[] MetalSalvageList = new string[30];
            #region Metal Bars
            //Albion
            MetalSalvageList[0] = "bronze_metal_bars";
            MetalSalvageList[1] = "iron_metal_bars";
            MetalSalvageList[2] = "steel_metal_bars";
            MetalSalvageList[3] = "alloy_metal_bars";
            MetalSalvageList[4] = "fine_alloy_metal_bars";
            MetalSalvageList[5] = "mithril_metal_bars";
            MetalSalvageList[6] = "adamantium_metal_bars";
            MetalSalvageList[7] = "asterite_metal_bars";
            MetalSalvageList[8] = "netherium_metal_bars";
            MetalSalvageList[9] = "arcanium_metal_bars";
            //Midgard
            MetalSalvageList[10] = "bronze_metal_bars";
            MetalSalvageList[11] = "iron_metal_bars";
            MetalSalvageList[12] = "steel_metal_bars";
            MetalSalvageList[13] = "alloy_metal_bars";
            MetalSalvageList[14] = "fine_alloy_metal_bars";
            MetalSalvageList[15] = "mithril_metal_bars";
            MetalSalvageList[16] = "adamantium_metal_bars";
            MetalSalvageList[17] = "asterite_metal_bars";
            MetalSalvageList[18] = "netherium_metal_bars";
            MetalSalvageList[19] = "arcanium_metal_bars";
            //Hibernia
            MetalSalvageList[20] = "copper_metal_bars";
            MetalSalvageList[21] = "ferrite_metal_bars";
            MetalSalvageList[22] = "quartz_metal_bars";
            MetalSalvageList[23] = "dolomite_metal_bars";
            MetalSalvageList[24] = "cobalt_metal_bars";
            MetalSalvageList[25] = "carbide_metal_bars";
            MetalSalvageList[26] = "sapphire_metal_bars";
            MetalSalvageList[27] = "diamond_metal_bars";
            MetalSalvageList[28] = "netherite_metal_bars";
            MetalSalvageList[29] = "arcanite_metal_bars";
            #endregion
            
            string[] JewelSalvageList = new string[33];
            #region Jewels
            JewelSalvageList[0] = "Alexandrite";
            JewelSalvageList[1] = "Jade";
            JewelSalvageList[2] = "Water_Opal";
            JewelSalvageList[3] = "Rhodolite";
            JewelSalvageList[4] = "Peridot";
            JewelSalvageList[5] = "Yellow_Tourmaline";
            JewelSalvageList[6] = "Kornerupine";
            JewelSalvageList[7] = "Purple_Sapphire";
            JewelSalvageList[8] = "Chrysoberyl";
            JewelSalvageList[9] = "Black_Sapphire";
            JewelSalvageList[10] = "Precious_Heliodor";
            #endregion
            
            int[] JewelCost = new int[11];
            #region Jewel Cost
            JewelCost[0] = 8;
            JewelCost[1] = 72;
            JewelCost[2] = 432;
            JewelCost[3] = 1728;
            JewelCost[4] = 5184;
            JewelCost[5] = 11664;
            JewelCost[6] = 17496;
            JewelCost[7] = 26248;
            JewelCost[8] = 34120;
            JewelCost[9] = 39232;
            JewelCost[10] = 39232;
            #endregion

            int[] MetalBarCost = new int[10];
            #region MetalCost
            MetalBarCost[0] = 8;
            MetalBarCost[1] = 140;
            MetalBarCost[2] = 560;
            MetalBarCost[3] = 1680;
            MetalBarCost[4] = 5040;
            MetalBarCost[5] = 10080;
            MetalBarCost[6] = 15120;
            MetalBarCost[7] = 22680;
            MetalBarCost[8] = 34020;
            MetalBarCost[9] = 51030;
            #endregion
            int[] WoodPlankCost = new int[10];
            #region Wooden Plank Cost
            WoodPlankCost[0] = 3;
            WoodPlankCost[1] = 58;
            WoodPlankCost[2] = 232;
            WoodPlankCost[3] = 696;
            WoodPlankCost[4] = 2088;
            WoodPlankCost[5] = 4176;
            WoodPlankCost[6] = 6264;
            WoodPlankCost[7] = 9396;
            WoodPlankCost[8] = 14094;
            WoodPlankCost[9] = 21141;
            #endregion
            int[] ClothSquareCost = new int[10];
            #region Cloth Square Cost
            ClothSquareCost[0] = 2;
            ClothSquareCost[1] = 38;
            ClothSquareCost[2] = 152;
            ClothSquareCost[3] = 456;
            ClothSquareCost[4] = 1368;
            ClothSquareCost[5] = 2736;
            ClothSquareCost[6] = 4104;
            ClothSquareCost[7] = 6156;
            ClothSquareCost[8] = 9236;
            ClothSquareCost[9] = 13851;
            #endregion
            int[] LeatherSquareCost = new int[10];
            #region Leather Square Cost
            LeatherSquareCost[0] = 2;
            LeatherSquareCost[1] = 56;
            LeatherSquareCost[2] = 224;
            LeatherSquareCost[3] = 672;
            LeatherSquareCost[4] = 2016;
            LeatherSquareCost[5] = 4032;
            LeatherSquareCost[6] = 6048;
            LeatherSquareCost[7] = 9072;
            LeatherSquareCost[8] = 13611;
            LeatherSquareCost[9] = 20412;
            #endregion

            switch (Realm)
            {
                #region Determine offset by player realm
                case eRealm.Midgard:
                    Material_Offset = 10; //Midgard
                    break;
                case eRealm.Hibernia:
                    Material_Offset = 20; //Hibernia
                    break;
                default:
                    Material_Offset = 0; //Albion
                    break;
                #endregion
            }

                #region Armor Type Switch 32 - 38 and 41 for Cloaks
                switch (ObjectType) //Armors
                {
                    case 41:    //Cloak Verified*
                        MaterialSize = 1; //Small
                        switch (Item.Item_Type)
                        {
                            case 26:
                                MaterialType = 0; //Cloth
                                #region Cloaks have 2 extensions 0 is default return
                                switch (Extension)
                                {
                                    case 0: //Cloak, Hooded Cloak
                                        Yield.Count = 4;
                                        break;
                                    default: //Dressy, Fancy, Regal, Fine Cloak all seem to return 22
                                        Yield.Count = 22;
                                        break;
                                }
                                #endregion
                                break;
                            case 33:
                            case 35:
                            case 29:
                            case 34:
                            case 36:
                            case 32:
                            case 24:
                                MaterialType = 5;
                                Yield.Count = 1;
                                break;
                            default:
                                Yield.Count = 0;
                                return Yield;
                        }
                        break;
                    case 32:    //Cloth Verified*
                        MaterialType = 0; //Cloth
                        #region Cloth Items have 2 extensions 0 is default return
                        switch (ItemType)
                        {
                            case 21: //Head Verified*
                                MaterialSize = 2; //Medium
                                #region Cloth Armor Has 2 Extensions
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 16;
                                        break;
                                    default:
                                        Yield.Count = 24;
                                        break;
                                }
                                #endregion
                                break;
                            case 22: //Hands Verified*
                                MaterialSize = 1; //Small
                                #region Cloth Armor Has 2 Extensions
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 8;
                                        break;
                                    default:
                                        Yield.Count = 8;
                                        break;
                                }
                                #endregion
                                break;
                            case 23: //Feet Verified*
                                MaterialSize = 1; //Small
                                #region Cloth Armor Has 2 Extensions
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 6;
                                        break;
                                    default:
                                        Yield.Count = 8;
                                        break;
                                }
                                #endregion
                                break;
                            case 25: //Torso Verified*
                                MaterialSize = 3; //Large
                                #region Cloth Armor Has 2 Extensions
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 33;
                                        break;
                                    default:
                                        Yield.Count = 40;
                                        break;
                                }
                                #endregion
                                break;
                            case 26: //Some cloaks fall into this category
                                MaterialSize = 1; //Small
                                #region Cloaks have 2 extensions 0 is default return
                                switch (Extension)
                                {
                                    case 0: //Cloak, Hooded Cloak
                                        Yield.Count = 4;
                                        break;
                                    default: //Dressy, Fancy, Regal, Fine Cloak all seem to return 22
                                        Yield.Count = 22;
                                        break;
                                }
                                #endregion
                                break;
                            case 27: //Legs Verified*
                                MaterialSize = 3; //Large
                                #region Cloth Armor Has 2 Extensions
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 20;
                                        break;
                                    default:
                                        Yield.Count = 24;
                                        break;
                                }
                                #endregion
                                break;
                            case 28: //Arms Verified*
                                MaterialSize = 2; //Medium
                                #region Cloth Armor Has 2 Extensions
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 13;
                                        break;
                                    default:
                                        Yield.Count = 16;
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                    case 33:    //Leather 33
                        MaterialType = 1; //Leather
                        #region ItemType Determines Slot
                        switch (ItemType)
                        {
                            case 21: //Head Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 9;
                                        break;
                                    case 1:
                                        Yield.Count = 12;
                                        break;
                                    case 2:
                                        Yield.Count = 25;
                                        break;
                                    default:
                                        Yield.Count = 30;
                                        break;
                                }
                                break;
                                #endregion
                            case 22: //Hands Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 3;
                                        break;
                                    case 1:
                                        Yield.Count = 4;
                                        break;
                                    case 2:
                                        Yield.Count = 8;
                                        break;
                                    default:
                                        Yield.Count = 10;
                                        break;
                                }
                                #endregion
                                break;
                            case 23: //Feet Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 3;
                                        break;
                                    case 1:
                                        Yield.Count = 4;
                                        break;
                                    case 2:
                                        Yield.Count = 8;
                                        break;
                                    default:
                                        Yield.Count = 10;
                                        break;
                                }
                                #endregion
                                break;
                            case 25: //Torso Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 15;
                                        break;
                                    case 1:
                                        Yield.Count = 21;
                                        break;
                                    case 2:
                                        Yield.Count = 42;
                                        break;
                                    default:
                                        Yield.Count = 50;
                                        break;
                                }
                                #endregion
                                break;
                            case 27: //Legs
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 9;
                                        break;
                                    case 1:
                                        Yield.Count = 12;
                                        break;
                                    case 2:
                                        Yield.Count = 25;
                                        break;
                                    default:
                                        Yield.Count = 30;
                                        break;
                                }
                                #endregion
                                break;
                            case 28: //Arms
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 6;
                                        break;
                                    case 1:
                                        Yield.Count = 8;
                                        break;
                                    case 2:
                                        Yield.Count = 16;
                                        break;
                                    default:
                                        Yield.Count = 20;
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                    case 34:    //Studded 34
                        MaterialType = 2; //Strips
                        #region ItemType Determines Slot
                        switch (ItemType)
                        {
                            case 21: //Head Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 8;
                                        break;
                                    case 1:
                                        Yield.Count = 8;
                                        break;
                                    case 2:
                                        Yield.Count = 16;
                                        break;
                                    default:
                                        Yield.Count = 19;
                                        break;
                                }
                                #endregion
                                break;
                            case 22: //Hands Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 1;
                                        break;
                                    case 1:
                                        Yield.Count = 2;
                                        break;
                                    case 2:
                                        Yield.Count = 5;
                                        break;
                                    default:
                                        Yield.Count = 6;
                                        break;
                                }
                                #endregion
                                break;
                            case 23: //Feet Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 1;
                                        break;
                                    case 1:
                                        Yield.Count = 2;
                                        break;
                                    case 2:
                                        Yield.Count = 5;
                                        break;
                                    default:
                                        Yield.Count = 6;
                                        break;
                                }
                                #endregion
                                break;
                            case 25: //Torso Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 9;
                                        break;
                                    case 1:
                                        Yield.Count = 13;
                                        break;
                                    case 2:
                                        Yield.Count = 27;
                                        break;
                                    default:
                                        Yield.Count = 32;
                                        break;
                                }
                                #endregion
                                break;
                            case 27: //Legs Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 4;
                                        break;
                                    case 1:
                                        Yield.Count = 8;
                                        break;
                                    case 2:
                                        Yield.Count = 16;
                                        break;
                                    default:
                                        Yield.Count = 19;
                                        break;
                                }
                                #endregion
                                break;
                            case 28: //Arms Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 3;
                                        break;
                                    case 1:
                                        Yield.Count = 5;
                                        break;
                                    case 2:
                                        Yield.Count = 10;
                                        break;
                                    default:
                                        Yield.Count = 12;
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                    case 35:    //Chain 35
                        MaterialType = 4; //Metal
                        #region ItemType Determines Slot
                        switch (ItemType)
                        {
                            case 21: //Head Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 11;
                                        break;
                                    case 1:
                                        Yield.Count = 20;
                                        break;
                                    case 2:
                                        Yield.Count = 34;
                                        break;
                                    default:
                                        Yield.Count = 41;
                                        break;
                                }
                                #endregion
                                break;
                            case 22: //Hands Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 3;
                                        break;
                                    case 1:
                                        Yield.Count = 6;
                                        break;
                                    case 2:
                                        Yield.Count = 11;
                                        break;
                                    default:
                                        Yield.Count = 13;
                                        break;
                                }
                                #endregion
                                break;
                            case 23: //Feet Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 3;
                                        break;
                                    case 1:
                                        Yield.Count = 6;
                                        break;
                                    case 2:
                                        Yield.Count = 11;
                                        break;
                                    default:
                                        Yield.Count = 13;
                                        break;
                                }
                                #endregion
                                break;
                            case 25: //Torso Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 19;
                                        break;
                                    case 1:
                                        Yield.Count = 33;
                                        break;
                                    case 2:
                                        Yield.Count = 57;
                                        break;
                                    default:
                                        Yield.Count = 69;
                                        break;
                                }
                                #endregion
                                break;
                            case 27: //Legs Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 11;
                                        break;
                                    case 1:
                                        Yield.Count = 20;
                                        break;
                                    case 2:
                                        Yield.Count = 34;
                                        break;
                                    default:
                                        Yield.Count = 41;
                                        break;
                                }
                                #endregion
                                break;
                            case 28: //Arms Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 7;
                                        break;
                                    case 1:
                                        Yield.Count = 13;
                                        break;
                                    case 2:
                                        Yield.Count = 23;
                                        break;
                                    default:
                                        Yield.Count = 27;
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                    case 36:    //Plate 36
                        MaterialType = 4; //Metal
                        #region ItemType Determines Slot
                        switch (ItemType)
                        {
                            case 21: //Head Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 17;
                                        break;
                                    case 1:
                                        Yield.Count = 27;
                                        break;
                                    case 2:
                                        Yield.Count = 38;
                                        break;
                                    default:
                                        Yield.Count = 38;
                                        break;
                                }
                                #endregion
                                break;
                            case 22: //Hands Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 6;
                                        break;
                                    case 1:
                                        Yield.Count = 9;
                                        break;
                                    case 2:
                                        Yield.Count = 14;
                                        break;
                                    default:
                                        Yield.Count = 13;
                                        break;
                                }
                                #endregion
                                break;
                            case 23: //Feet Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 6;
                                        break;
                                    case 1:
                                        Yield.Count = 9;
                                        break;
                                    case 2:
                                        Yield.Count = 14;
                                        break;
                                    default:
                                        Yield.Count = 13;
                                        break;
                                }
                                #endregion
                                break;
                            case 25: //Torso Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 27;
                                        break;
                                    case 1:
                                        Yield.Count = 45;
                                        break;
                                    case 2:
                                        Yield.Count = 64;
                                        break;
                                    default:
                                        Yield.Count = 66;
                                        break;
                                }
                                #endregion
                                break;
                            case 27: //Legs Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 17;
                                        break;
                                    case 1:
                                        Yield.Count = 27;
                                        break;
                                    case 2:
                                        Yield.Count = 38;
                                        break;
                                    default:
                                        Yield.Count = 38;
                                        break;
                                }
                                #endregion
                                break;
                            case 28: //Arms Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 11;
                                        break;
                                    case 1:
                                        Yield.Count = 18;
                                        break;
                                    case 2:
                                        Yield.Count = 26;
                                        break;
                                    default:
                                        Yield.Count = 26;
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                    case 37:    //Reinforced 37
                        MaterialType = 2; //Strips
                        #region ItemType Determines Slot
                        switch (ItemType)
                        {
                            case 21: //Head Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 5;
                                        break;
                                    case 1:
                                        Yield.Count = 8;
                                        break;
                                    case 2:
                                        Yield.Count = 16;
                                        break;
                                    default:
                                        Yield.Count = 19;
                                        break;
                                }
                                #endregion
                                break;
                            case 22: //Hands Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 1;
                                        break;
                                    case 1:
                                        Yield.Count = 2;
                                        break;
                                    case 2:
                                        Yield.Count = 5;
                                        break;
                                    default:
                                        Yield.Count = 6;
                                        break;
                                }
                                #endregion
                                break;
                            case 23: //Feet Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 1;
                                        break;
                                    case 1:
                                        Yield.Count = 2;
                                        break;
                                    case 2:
                                        Yield.Count = 5;
                                        break;
                                    default:
                                        Yield.Count = 6;
                                        break;
                                }
                                #endregion
                                break;
                            case 25: //Torso Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 9;
                                        break;
                                    case 1:
                                        Yield.Count = 13;
                                        break;
                                    case 2:
                                        Yield.Count = 32;
                                        break;
                                    default:
                                        Yield.Count = 32;
                                        break;
                                }
                                #endregion
                                break;
                            case 27: //Legs Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 5;
                                        break;
                                    case 1:
                                        Yield.Count = 8;
                                        break;
                                    case 2:
                                        Yield.Count = 16;
                                        break;
                                    default:
                                        Yield.Count = 19;
                                        break;
                                }
                                #endregion
                                break;
                            case 28: //Arms Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 3;
                                        break;
                                    case 1:
                                        Yield.Count = 5;
                                        break;
                                    case 2:
                                        Yield.Count = 10;
                                        break;
                                    default:
                                        Yield.Count = 12;
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                    case 38:    //Scale 38
                        MaterialType = 4; //Metal
                        #region ItemType Determines Slot
                        switch (ItemType)
                        {
                            case 21: //Head Verified*
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 11;
                                        break;
                                    case 1:
                                        Yield.Count = 20;
                                        break;
                                    case 2:
                                        Yield.Count = 34;
                                        break;
                                    default:
                                        Yield.Count = 41;
                                        break;
                                }
                                #endregion
                                break;
                            case 22: //Hands Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 3;
                                        break;
                                    case 1:
                                        Yield.Count = 6;
                                        break;
                                    case 2:
                                        Yield.Count = 11;
                                        break;
                                    default:
                                        Yield.Count = 13;
                                        break;
                                }
                                #endregion
                                break;
                            case 23: //Feet Verified*
                                MaterialSize = 1; //Small
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 3;
                                        break;
                                    case 1:
                                        Yield.Count = 6;
                                        break;
                                    case 2:
                                        Yield.Count = 11;
                                        break;
                                    default:
                                        Yield.Count = 13;
                                        break;
                                }
                                #endregion
                                break;
                            case 25: //Torso Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 19;
                                        break;
                                    case 1:
                                        Yield.Count = 33;
                                        break;
                                    case 2:
                                        Yield.Count = 57;
                                        break;
                                    default:
                                        Yield.Count = 69;
                                        break;
                                }
                                #endregion
                                break;
                            case 27: //Legs Verified*
                                MaterialSize = 3; //Large
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 11;
                                        break;
                                    case 1:
                                        Yield.Count = 20;
                                        break;
                                    case 2:
                                        Yield.Count = 34;
                                        break;
                                    default:
                                        Yield.Count = 41;
                                        break;
                                }
                                #endregion
                                break;
                            case 28: //Arms Verified
                                MaterialSize = 2; //Medium
                                #region 0 is default return value
                                switch (Extension)
                                {
                                    case 0:
                                        Yield.Count = 7;
                                        break;
                                    case 1:
                                        Yield.Count = 13;
                                        break;
                                    case 2:
                                        Yield.Count = 23;
                                        break;
                                    default:
                                        Yield.Count = 27;
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                }
                #endregion

                #region Weapon Type Switch 2 - 28

                switch (ObjectType) //Weapons
                {
                    case 2: //Crushing Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46

                                Yield.Count = 7; //Albion Hammer
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 11; //Albion Mace
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 14; //Albion Flanged Mace
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 16; //Albion Spiked Mace
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    26; //Albion exceptional mace,needle mace, exceptional hammer,War Mace, fortified mace, Fortified Hammer
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 21; //Albion War Hammer
                                break;
                        }

                        break;
                    case 3: //Slashing Weapons 
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 7; //Albion Short Swords
                                if (Item.SPD_ABS <= 24) Yield.Count = 6; //Albion Daggers
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 14; //Albion Broadsword (guess)
                                if (Item.SPD_ABS <= 27) Yield.Count = 10; //Albion Handaxe
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 15; //Albion scimatar
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 19; //Albion long sword
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    27; //Albion Exceptional long Sword, Fortified Long Sword, exceptional short sword, Fortified Short Sword, Jambiya
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 23; //Albion Bastardsword
                                if (Item.SPD_ABS <= 36) Yield.Count = 27; //Albion Sabre
                                break;
                        }

                        break;
                    case 4: //Thrusting Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 6; //Albion dirk
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 9; //Albion silletto
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 14; //Albion main gauche
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 16; //Albion rapier
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    26; //Albion Exceptional Rapier, guarded rapier, exceptional stilletto, Fortified Rapier, long dirk, Fortified Silletto
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 21; //Albion gladius
                                break;
                        }

                        break;
                    case 5: //Fired Weapon, Short Bow
                        MaterialType = 3; //Return Type is Wood
                        MaterialSize = 1; //Small
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 15; //Albion short bow
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 15; //(Guess)
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 15; //Hibernia short bow
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 15; //(Guess)
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    8; //Albion, Hibernia heavy short bow, exceptional short bow, fortified short bow
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 12; //Albion hunting bow
                                break;
                        }

                        break;
                    case 6: //2-Handed Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 11; //Albion War Mattock
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 14; //Albion two handed sword
                                if (Item.SPD_ABS <= 44) Yield.Count = 20; //Albion War Axe
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 18; //Albion great hammer
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 24; //Albion great sword
                                if (Item.SPD_ABS <= 45) Yield.Count = 21; //Albion battle axe
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    32; //Albion archmace, exceptional great hammer, Exceptional War Pick, Exceptional great Sword, war pick, great scimitar, Fortified Great Hammer, Fortified War Pick, Fortified Great Sword
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 27; //Albion great axe
                                break;
                        }

                        break;
                    case 7: //Polearm Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 10; //Albion pike
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 12; //Albion lochaber axe
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 15; //Albion bill
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 23; //Albion lucerne hammer
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    30; //Albion Spiked Hammer, pole axe, Exceptional Partisan, Exceptional Lucerne Hammer, Bardiche, partisan, Fortified Lucerne hammer, Exceptional Bardiche, Fortified Partisan
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 25; //Albion halbred
                                break;
                        }

                        break;
                    case 8: //Staffs
                        MaterialType = 3; //Return Type is Wood
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 24; //Midgard, Albion, Hibernia staff
                                if (Item.SPD_ABS <= 30) Yield.Count = 16; //Albion quarterstaff
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 16; //(guess)
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                switch (Realm)
                                {
                                    case eRealm.Albion:
                                        Yield.Count = 22; //Albion shod staff
                                        if (Item.SPD_ABS <= 42) Yield.Count = 26; //Albion shod quarterstaff
                                        break;
                                    case eRealm.Midgard:
                                        Yield.Count = 24; //Midgard shod staff
                                        break;
                                    default:
                                        Yield.Count = 24; //Hibernia shod staff
                                        break;
                                }

                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 24; //(guess)
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    39; //Midgard, Albion, Hibernia heavy shod staff, heavy shod quarterstaff, exceptional quarterstaff, exceptional magus staff
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 24; //(guess)
                                break;
                        }

                        break;
                    case 9: //LongBow Weapons
                        MaterialType = 3; //Return Type is Wood
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 51: //Levels 0,51+
                                Yield.Count = 39; //Albion heavy longbow, exceptional longbow, fortified longbow
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 22; //Albion long bow
                                if (Item.SPD_ABS <= 47) Yield.Count = 16; //Albion bow
                                if (Item.SPD_ABS <= 40) Yield.Count = 12; //Albion hunting bow
                                break;
                        }

                        break;
                    case 10: //Crossbow
                        MaterialType = 3; //Return Type is Wood
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            //These values are only a guess and verified not to return more material then cost
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    22; //(guess) Closest material match is shod staff - Albion light crossbow, exceptional crossbow, fortified crossbow
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 22; //Based on Albion long bow
                                if (Item.SPD_ABS <= 47) Yield.Count = 16; //Based on Albion bow
                                if (Item.SPD_ABS <= 40) Yield.Count = 12; //Based on Albion hunting bow
                                break;
                        }

                        break;
                    case 11: //Sword Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 7; //Midgard dagger
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 10; //Midgard short sword
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 14; //Midgard broadsword
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 19; //Midgard long sword
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count = 28; //Midgard dwarven short sword
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 23; //Midgard bastard sword
                                break;
                        }

                        break;
                    case 12: //Hammer Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 7; //Midgard small hammer
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 12; //Midgard hammer
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 15; //Midgard war hammer
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 20; //Midgard pick hammer
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count = 28; //Midgard spiked hammer
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 25; //Midgard battle hammer
                                break;
                        }

                        break;
                    case 13: //Axe Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 7; //Midgard hand axe
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 11; //Midgard bearded axe
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 15; //Midgard war axe
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 19; //Midgard spiked axe
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count = 27; //Midgard cleaver
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 23; //Midgard doublebladed axe
                                break;
                        }

                        break;
                    case 14: //Spear Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 10; //Midgard spear
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 12; //Midgard long spear
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 15; //Midgard trident
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 20; //Midgard lugged spear
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count = 32; //Midgard battle spear
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 27; //Midgard great spear
                                break;
                        }

                        break;
                    case 15: //Composite Bow Weapon
                        MaterialType = 3; //Return Type is Wood
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    28; //Midgard heavy composite bow, exceptional composite bow, fortified composite bow
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 16; //Midgard great composite bow
                                if (Item.SPD_ABS <= 40) Yield.Count = 12; //Midgard composite bow
                                break;
                        }

                        break;
                    case 16: //Throwing Weapons
                        //Only cost 1 metal bar to create
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 0; //Do not return salvage
                        Yield.Count = 0; //Midgard weighted throwing knives
                        break;
                    case 17: //Left Axe Weapons
                        //Verified in old mythic database all left axe return 0 unless made of alloy and up
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 0; //Don't return small, medium or large metal
                        Yield.Count = Item.Level < 20 ? 0 : 1;
                        break;
                    case 18: //Recurve Bow Weapons
                        MaterialType = 3; //Return Type is Wood
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    39; //Albion heavy recurve bow, Hibernia exceptional recurve bow, Hibernia fortified recurve bow
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 22; //Albion great recurve bow
                                if (Item.SPD_ABS <= 47) Yield.Count = 16; //Albion recurve bow
                                if (Item.SPD_ABS <= 40) Yield.Count = 12; //Albion short recurve bow
                                break;
                        }

                        break;
                    case 19: //Blades Weapon
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 7; //Hibernia Short sword
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 11; //Hibernia Falcata
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 14; //Hibernia Broadsword
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 19; //Hibernia Longsword
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    27; //Hibernia weighted long sword, exceptional long sword, fortified long sword, crecent sword, sickle, exceptional short sword
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 23; //Hibernia Bastard sword
                                break;
                        }

                        break;
                    case 20: //Blunt Weapon
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 7; //Hibernia club
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 11; //Hibernia mace
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 14; //Hibernia spiked club
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 16; //Hibernia hammer
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    26; //Hibernia Exceptional mace, pick hammer, fortified mace, exceptional hammer, barbed mace, fortified hammer
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 21; //Hibernia spiked mace
                                break;
                        }

                        break;
                    case 21: //Peircing Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 6; //Hibernia dirk
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 9; //Hibernia dagger
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 14; //Hibernia stilletto
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 16; //Hibernia rapier
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    26; //Hibernia Exceptional Rapier, guarded rapier, Exceptional stilletto, fortified rapier, angled dagger, fortified stilletto
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 21; //Hibernia curved dagger
                                break;
                        }

                        break;
                    case 22: //Large Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 24; //Hibernia great hammer
                                if (Item.SPD_ABS <= 50) Yield.Count = 23; //Hibernia two-handed spiked mace
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    32; //Hibernia exceptional great sword, sledge hammer, exceptional great hammer, fortified great sword, fortified great hammer, great falcata
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 27; //Hibernia great sword

                                //No items match case 1 or 4 so adding case 2 to default
                                if (Item.SPD_ABS <= 47) Yield.Count = 18; //Hibernia big shillelagh
                                if (Item.SPD_ABS <= 46) Yield.Count = 14; //Hibernia ghjf
                                break;
                        }

                        break;
                    case 23: //Celtic Spear Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 3; //Large
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 7; //Hibernia short spear
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 11; //Hibernia spear
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 16; //Hibernia long spear
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 19; //Hibernia war spear
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count = 32; //Hibernia exceptional battle spear
                                if (Item.SPD_ABS <= 50) Yield.Count = 32; //Hibernia hooked spear
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 21; //Hibernia barbed spear
                                break;
                        }

                        break;
                    case 24: //Flexable Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 9; //Albion barbed chain, Chain
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 14; //Albion Morning Star, tipped whip
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 16; //Albion War Chain, blade tipped whip
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    23; //Albion weighted flail, Exceptional Chain, Exceptional Whip, Fortified Chain, thick barbed whip, Fortified Whip
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 19; //Albion Flail, barbed whip
                                break;
                        }

                        break;
                    case 25: //Hand to Hand Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 1; //Small
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 9; //Midgard moon fang
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 11; //Midgard moon claw
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 14; //Midgard bladed moon fang
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count = 20; //Midgard heavy bladed claw greave
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 14; //Midgard bladed moon claw
                                break;
                        }

                        break;
                    case 26: //Scythe Weapons
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 3; //Large???
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 10; //Hibernia scythe
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 12; //Hibernia havest scythe
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                Yield.Count = 15; //Hibernia maritial scythe
                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 20; //Hibernia war skythe
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count = 32; //Hibernia moon crested war skythe
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 27; //Hibernia great war skythe
                                break;
                        }

                        break;
                    case 27: //Fistwrap Weapons - Mauler
                        MaterialType = 4; //Return Type is Metal
                        MaterialSize = 1; //Small
                        Yield.Count = 0; //No data avail dont return salvage
                        break;
                    case 28: //Staff Weapons - Mauler (Copy of Staff salvage rates)
                        MaterialType = 3; //Return Type is Wood
                        MaterialSize = 2; //Medium
                        switch (SubTier)
                        {
                            case 1: //Levels 1,6,11,16,21,26,31,36,41,46
                                Yield.Count = 24; //Midgard, Albion, Hibernia staff
                                if (Item.SPD_ABS <= 30) Yield.Count = 16; //Albion quarterstaff
                                break;
                            case 2: //Levels 2,7,12,17,22,27,32,37,42,47
                                Yield.Count = 16; //(guess)
                                break;
                            case 3: //Levels 3,8,13,18,23,28,33,38,43,48
                                switch (Realm)
                                {
                                    case eRealm.Albion:
                                        Yield.Count = 22; //Albion shod staff
                                        if (Item.SPD_ABS <= 42) Yield.Count = 26; //Albion shod quarterstaff
                                        break;
                                    case eRealm.Midgard:
                                        Yield.Count = 24; //Midgard shod staff
                                        break;
                                    default:
                                        Yield.Count = 24; //Hibernia shod staff
                                        break;
                                }

                                break;
                            case 4: //Levels 4,9,14,19,24,29,34,39,44,49
                                Yield.Count = 24; //(guess)
                                break;
                            case 51: //Levels 0,51+
                                Yield.Count =
                                    39; //Midgard, Albion, Hibernia heavy shod staff, heavy shod quarterstaff, exceptional quarterstaff, exceptional magus staff
                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                Yield.Count = 24; //(guess)
                                break;
                        }

                        break;
                    case 42: //Sheilds
                        MaterialType = 4; //Return Type is Metal
                        switch (SubTier)
                        {
                            case 51: //Levels 0,51+
                                switch (Item.Type_Damage)
                                {
                                    case 1: //Small Shield
                                        MaterialSize = 1; //Small
                                        Yield.Count = 3; //Albion exceptional buckler
                                        break;
                                    case 2: //Medium Shield
                                        MaterialSize = 2; //Medium
                                        Yield.Count = 3; //Albion exceptional heater sheild
                                        break;
                                    default: //Large Shield
                                        MaterialSize = 3; //Large
                                        Yield.Count = 4; //Albion exceptional block sheild
                                        break;
                                }

                                break;
                            default: //Levels 5,10,15,20,25,30,35,40,45,50
                                switch (Item.Type_Damage)
                                {
                                    case 1: //Small Shield
                                        MaterialSize = 1; //Small
                                        Yield.Count = 2; //Albion round sheild
                                        break;
                                    case 2: //Medium Shield
                                        MaterialSize = 2; //Medium
                                        Yield.Count = 3; //Albion exceptional heater sheild
                                        break;
                                    default: //Large Shield
                                        MaterialSize = 3; //Large
                                        Yield.Count = 4; //Albion heater sheild
                                        break;
                                }

                                break;
                        }

                        break;
                    case 45: //Harps, Drums, Lutes, Flutes
                        MaterialType = 3; //Return Type is Wood
                        MaterialSize = 2; //Medium
                        Yield.Count = 18; //Albion drum, lute, flute
                        break;
                    default:
                        //DO NOT PUT CODE HERE, THIS IS A FALL THROUGH
                        break;
                }

                #endregion
            

            bool DropPenalty = false;
            //Non crafted items do not have the same worth as crafted in metal bars
            if (!Item.IsCrafted && Yield.Tier > 0 && Item.Level < 49)
            {
                --Yield.Tier; DropPenalty = true;
            }

            switch (MaterialType)
            {
                case 0: //Cloth
                    Yield.ID = ClothSalvageList[Material_Offset + Yield.Tier];
                    Yield.Count = Yield.Count < 1 ? 0 : Yield.Count;
                    Yield.MSRP = DropPenalty == true ? (ClothSquareCost[Yield.Tier + 1] * Yield.Count) * 2 : (ClothSquareCost[Yield.Tier] * Yield.Count) * 2;

                    #region //Select correct material size to return
                    switch(MaterialSize)
                    {
                        case 1: //Small
                            Difference += 18;
                            break;
                        case 2: //Medium
                            Difference += 36;
                            break;
                        case 3: //Large
                            Difference += 54;
                            break;
                        default: //Dont reurn
                            Difference = 0;
                            break;
                    }
    #endregion
                    Yield.Count = Item.IsCrafted == true ? Yield.Count : Difference;
                    break;
                case 1: //Leather
                    Yield.ID = LeatherSalvageList[Material_Offset + Yield.Tier];
                    Yield.Count = Yield.Count < 1 ? 0 : Yield.Count;
                    Yield.MSRP = DropPenalty == true ? (LeatherSquareCost[Yield.Tier + 1] * Yield.Count) * 2 : (LeatherSquareCost[Yield.Tier] * Yield.Count) * 2;

                    #region //Select correct material size to return
                    switch(MaterialSize)
                    {
                        case 1: //Small
                            Difference += 18;
                            break;
                        case 2: //Medium
                            Difference += 36;
                            break;
                        case 3: //Large
                            Difference += 54;
                            break;
                        default: //Dont reurn
                            Difference = 0;
                            break;
                    }
                    #endregion
                    Yield.Count = Item.IsCrafted == true ? Yield.Count : Difference;
                    break;
                case 2: //Strips or Metal
                    Yield.ID = StripsSalvageList[Material_Offset + Yield.Tier];
                    Yield.Count = Yield.Count < 1 ? 0 : Yield.Count;
                    Yield.MSRP = DropPenalty == true ? (MetalBarCost[Yield.Tier + 1] * Yield.Count) * 2 : (MetalBarCost[Yield.Tier] * Yield.Count) * 2;

                    #region //Select correct material size to return
                    switch(MaterialSize)
                    {
                        case 1: //Small
                            Difference += 5;
                            break;
                        case 2: //Medium
                            Difference += 10;
                            break;
                        case 3: //Large
                            Difference += 15;
                            break;
                        default: //Dont reurn
                            Difference = 0;
                            break;
                    }
                    #endregion
                    Yield.Count = Item.IsCrafted == true ? Yield.Count : Difference;
                    break;
                case 3: //Wood
                    Yield.ID = WoodSalvageList[Material_Offset + Yield.Tier];
                    Yield.Count = Yield.Count < 1 ? 0 : Yield.Count;
                    Yield.MSRP = DropPenalty == true ? (WoodPlankCost[Yield.Tier + 1] * Yield.Count) * 2 : (WoodPlankCost[Yield.Tier] * Yield.Count) * 2;

                    #region //Select correct material size to return
                    switch (MaterialSize)
                    {
                        case 1: //Small
                            Difference += 12;
                            break;
                        case 2: //Medium
                            Difference += 24;
                            break;
                        case 3: //Large
                            Difference += 36;
                            break;
                        default: //Dont reurn
                            Difference = 0;
                            break;
                    }
                    #endregion
                    Yield.Count = Item.IsCrafted == true ? Yield.Count : Difference;
                    break;
                case 4: //Metal
                    Yield.ID = MetalSalvageList[Material_Offset + Yield.Tier];
                    Yield.Count = Yield.Count < 1 ? 0 : Yield.Count;
                    Yield.MSRP = DropPenalty == true ? (MetalBarCost[Yield.Tier + 1] * Yield.Count) * 2 : (MetalBarCost[Yield.Tier] * Yield.Count) * 2;

                    #region //Select correct material size to return
                    switch(MaterialSize)
                    {
                        case 1: //Small
                            Difference += 5;
                            break;
                        case 2: //Medium
                            Difference += 10;
                            break;
                        case 3: //Large
                            Difference += 15;
                            break;
                        default: //Dont return
                            Difference = 0;
                            break;
                    }
                    #endregion
                    Yield.Count = Item.IsCrafted == true ? Yield.Count : Difference;
                    break;
                case 5: //Magicals
                    Yield.ID = JewelSalvageList[Yield.Tier];
                    Yield.Count = 1;
                    Yield.MSRP = DropPenalty == true ? (JewelCost[Yield.Tier + 1] * Yield.Count) * 2 : (JewelCost[Yield.Tier] * Yield.Count) * 2;
                    
                    break;
                default: //Will not return any salvage but provides some info
                    //Provide a MSRP if possible based on fallthrough results
                    Yield.ID = MetalSalvageList[Material_Offset + Yield.Tier];
                    Yield.Count = Yield.Count < 1 ? 0 : Yield.Count;
                    Yield.MSRP = DropPenalty == true ? (MetalBarCost[Yield.Tier + 1] * Yield.Count) * 2 : (MetalBarCost[Yield.Tier] * Yield.Count) * 2;

                    Yield.Count = 0; //Lowest return value
                    Yield.ID = ClothSalvageList[0]; //Lowest return value thats not null
                    break;
            }

            Yield.Skill = (Yield.Tier * 100) + 15;

            //Make underscores in to spaces
            Yield.Material = Yield.ID.Replace("_", " ");
            //Strip out any special character or number
            Yield.Material = Regex.Replace(Yield.Material,@"[\d-]",string.Empty);

                switch (Item.Price)
                {
                    case 0:
                        if (Player.Client.Account.PrivLevel != 1)
                        {
                            Player.Out.SendDebugMessage("Items with a price set to 0 cannot be salvaged or sold to merchants, the MSRP for this item is " + Yield.MSRP + "c Use /setsalvage if return value needs changed");
                        }
                        Yield.Count = 0;
                        break;
                    default:
                        if (Player.Client.Account.PrivLevel != 1)
                        {
                            Player.Out.SendDebugMessage("CALCULATOR: ObjectType " + ObjectType + " ItemType " + ItemType + " Count " + Yield.Count + " Material " + Yield.Material + " Item.Price " + Item.Price + " MSRP " + Yield.MSRP);
                        }
                        
                        break;
                }
                
                #region AtlasROGs
                
                if (Description.Contains("Atlas ROG"))
                {
                    Yield.Count = 2;
                }
                
                #endregion
            return Yield;
        }

        //Not in use
        private int CalculateLoss(GamePlayer player, int SubSkill, int ItemSkill, int SubTier)
        {
            //TODO
            return 0;
        }
    }
}



