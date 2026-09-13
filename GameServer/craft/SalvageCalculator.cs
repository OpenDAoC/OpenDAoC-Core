/*
 * The following changes to item extensions are needed in the database
 * for this calculator to work.
 *
 * ALBION ARMOR EXTENSIONS
 *   Cloth       0 Quilted            Cloth2   1 Double Stitched
 *   Leather     0 Roman              Leather  1 Cymric             Leather 2 Siluric            Leather 3 Padded Leather
 *   Studded     0 Studded            Studded  1 Boned              Studded 2 Lamellar           Studded 3 Reinforced Lamellar
 *   Chain1      0 Chain (Chain2 unused in Albion)
 *   Chain3      2 Mail               Chain4   3 Improved Mail
 *   Plate1      0 Plate              Plate2   1 Scaled Plate       Plate3 2 Fluted Plate        Plate4 3 Full Plate
 *
 * HIBERNIA ARMOR EXTENSIONS
 *   Cloth       0 Woven              Cloth2   1 Thick Woven
 *   Leather1    0 Brea               Leather2 1 Constaic           Leather3 2 Cruaigh           Leather4 3 Padded Cruaigh
 *   Reinforced1 0 Tacuil             Reinforced2 1 Nadurtha        Reinforced3 2 Cailiocht      Reinforced4 3 Heavy Cailiocht
 *   Scale1      0 Cruanach           Scale2   1 Daingean           Scale3 2 Osnadurtha          Scale4 3 Improved Osnadurtha
 *   Plate2      1 Scaled Plate       Plate3   2 Fluted Plate       Plate4 3 Full Plate
 *
 * MIDGARD ARMOR EXTENSIONS
 *   Cloth       0 Padded             Cloth2   1 Thick Padded
 *   Leather1    0 Mjuklaedar         Leather2 1 Svarlaedar         Leather3 2 Starklaedar       Leather4 3 Padded Starklaedar
 *   Studded1    0 Stelskodd          Studded2 1 Svarskodd          Studded3 2 Starkaskodd       Studded4 3 Heavy Starkaskodd
 *   Chain1      0 Pansarkedja        Chain2   1 Svarkedja          Chain3 2 Starkakedja         Chain4 3 Heavy Starkakedja
 *   Plate2      1 Scaled Plate       Plate3   2 Fluted Plate       Plate4 3 Full Plate
 *
 * Reference SQL used to backfill the Extension column from the old Id_nb naming scheme:
 *
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%double_stitched%";
 * Update databasename.itemtemplate set Extension = 0 where Id_nb like "%double_stitched_quilted_boots";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%cymric%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%boned%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%scaled_plate%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%thick_woven%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%constaic%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%nadurtha%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%daingean%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%thick_padded%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%svarlaedar%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%svarskodd%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%svarkedja%";
 * Update databasename.itemtemplate set Extension = 1 where Id_nb like "%spiked_circlet%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%siluric%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%lamellar%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%mail%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%fluted_plate%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%cruaigh%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%cailiocht%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%osnadurtha%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%starklaedar%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%starkaskodd%";
 * Update databasename.itemtemplate set Extension = 2 where Id_nb like "%starkakedja%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%padded_leather%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%reinforced_lamellar%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%improved_mail%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%full_plate%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%padded_cruaigh%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%heavy_cailiocht%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%improved_osnadurtha%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%padded_starklaedar%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%heavy_starkaskodd%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%heavy_starkakedja%";
 * Update databasename.itemtemplate set Extension = 3 where Id_nb like "%superior_war_circlet%";
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DOL.Database;
using DOL.GS.ServerProperties;

namespace DOL.GS.SalvageCalc
{
    public struct SalvageReturn
    {
        public string ID;
        public string Material;
        public int Count;
        public int Tier;
        public int Level;
        public int Skill;
        public int MSRP;
    }

    public static class SalvageCalculator
    {
        private enum MaterialType
        {
            Cloth = 0,
            Leather = 1,
            Strips = 2,
            Wood = 3,
            Metal = 4,
            Jewel = 5
        }

        private enum MaterialSize
        {
            None = 0,
            Small = 1,
            Medium = 2,
            Large = 3
        }

        private static class ArmorObjectType
        {
            public const int Cloth = 32;
            public const int Leather = 33;
            public const int Studded = 34;
            public const int Chain = 35;
            public const int Plate = 36;
            public const int Reinforced = 37;
            public const int Scale = 38;
            public const int Cloak = 41;
        }

        private static class EquipSlot
        {
            public const int Head = 21;
            public const int Hands = 22;
            public const int Feet = 23;
            public const int Torso = 25;
            public const int SubCloak = 26; // Some cloaks are typed as cloth items.
            public const int Legs = 27;
            public const int Arms = 28;
        }

        // Indexed by [RealmOffset + Tier]. Albion = 0-9, Midgard = 10-19, Hibernia = 20-29.
        private static readonly string[] ClothSalvage =
        [
            "woolen_cloth_squares", "linen_cloth_squares", "brocade_cloth_squares", "silk_cloth_squares",
            "gossamer_cloth_squares", "sylvan_cloth_squares", "seamist_cloth_squares", "nightshade_cloth_squares",
            "wyvernskin_cloth_squares", "silksteel_cloth_squares",

            "woolen_cloth_squares", "linen_cloth_squares", "brocade_cloth_squares", "silk_cloth_squares",
            "gossamer_cloth_squares", "sylvan_cloth_squares", "seamist_cloth_squares", "nightshade_cloth_squares",
            "wyvernskin_cloth_squares", "silksteel_cloth_squares",

            "woolen_cloth_squares", "linen_cloth_squares", "brocade_cloth_squares", "silk_cloth_squares",
            "gossamer_cloth_squares", "sylvan_cloth_squares", "seamist_cloth_squares", "nightshade_cloth_squares",
            "wyvernskin_cloth_squares", "silksteel_cloth_squares",
        ];

        private static readonly string[] LeatherSalvage =
        [
            "rawhide_leather_squares", "tanned_leather_squares", "cured_leather_squares", "hard_leather_squares",
            "rigid_leather_squares", "embossed_leather_squares", "imbued_leather_squares", "runed_leather_squares",
            "eldritch_leather_squares", "tempered_leather_squares",

            "rawhide_leather_squares", "tanned_leather_squares", "cured_leather_squares", "hard_leather_squares",
            "rigid_leather_squares", "embossed_leather_squares", "imbued_leather_squares", "runed_leather_squares",
            "eldritch_leather_squares", "tempered_leather_squares",

            "rawhide_leather_squares", "tanned_leather_squares", "cured_leather_squares", "hard_leather_squares",
            "rigid_leather_squares", "embossed_leather_squares", "imbued_leather_squares", "runed_leather_squares",
            "eldritch_leather_squares", "tempered_leather_squares",
        ];

        // Albion/Midgard use metal bars for "strips"; Hibernia uses organic strip materials.
        private static readonly string[] StripsSalvage =
        [
            "bronze_metal_bars", "iron_metal_bars", "steel_metal_bars", "alloy_metal_bars",
            "fine_alloy_metal_bars", "mithril_metal_bars", "adamantium_metal_bars", "asterite_metal_bars",
            "netherium_metal_bars", "arcanium_metal_bars",

            "bronze_metal_bars", "iron_metal_bars", "steel_metal_bars", "alloy_metal_bars",
            "fine_alloy_metal_bars", "mithril_metal_bars", "adamantium_metal_bars", "asterite_metal_bars",
            "netherium_metal_bars", "arcanium_metal_bars",

            "leaf_strips", "bone_strips", "vine_strips", "shell_strips",
            "fossil_strips", "amber_strips", "coral_strips", "chitin_strips",
            "petrified_strips", "crystalized_strips",
        ];

        private static readonly string[] WoodSalvage =
        [
            "rowan_wooden_boards", "elm_wooden_boards", "oaken_wooden_boards", "ironwood_wooden_boards",
            "heartwood_wooden_boards", "runewood_wooden_boards", "stonewood_wooden_boards", "ebonwood_wooden_boards",
            "dyrwood_wooden_boards", "duskwood_wooden_boards",

            "rowan_wooden_boards", "elm_wooden_boards", "oaken_wooden_boards", "ironwood_wooden_boards",
            "heartwood_wooden_boards", "runewood_wooden_boards", "stonewood_wooden_boards", "ebonwood_wooden_boards",
            "dyrwood_wooden_boards", "duskwood_wooden_boards",

            "rowan_wooden_boards", "elm_wooden_boards", "oaken_wooden_boards", "ironwood_wooden_boards",
            "heartwood_wooden_boards", "runewood_wooden_boards", "stonewood_wooden_boards", "ebonwood_wooden_boards",
            "dyrwood_wooden_boards", "duskwood_wooden_boards",
        ];

        private static readonly string[] MetalSalvage =
        [
            "bronze_metal_bars", "iron_metal_bars", "steel_metal_bars", "alloy_metal_bars",
            "fine_alloy_metal_bars", "mithril_metal_bars", "adamantium_metal_bars", "asterite_metal_bars",
            "netherium_metal_bars", "arcanium_metal_bars",

            "bronze_metal_bars", "iron_metal_bars", "steel_metal_bars", "alloy_metal_bars",
            "fine_alloy_metal_bars", "mithril_metal_bars", "adamantium_metal_bars", "asterite_metal_bars",
            "netherium_metal_bars", "arcanium_metal_bars",

            "copper_metal_bars", "ferrite_metal_bars", "quartz_metal_bars", "dolomite_metal_bars",
            "cobalt_metal_bars", "carbide_metal_bars", "sapphire_metal_bars", "diamond_metal_bars",
            "netherite_metal_bars", "arcanite_metal_bars",
        ];

        // Jewels aren't realm-specific: indexed directly by Tier (0-9).
        private static readonly string[] JewelSalvage =
        [
            "Alexandrite", "Jade", "Water_Opal", "Rhodolite", "Peridot",
            "Yellow_Tourmaline", "Kornerupine", "Purple_Sapphire", "Chrysoberyl", "Black_Sapphire",
        ];

        private static readonly int[] JewelCost = [8, 72, 432, 1728, 5184, 11664, 17496, 26248, 34120, 39232, 39232];
        private static readonly int[] MetalBarCost = [8, 140, 560, 1680, 5040, 10080, 15120, 22680, 34020, 51030];
        private static readonly int[] WoodPlankCost = [3, 58, 232, 696, 2088, 4176, 6264, 9396, 14094, 21141];
        private static readonly int[] ClothSquareCost = [2, 38, 152, 456, 1368, 2736, 4104, 6156, 9236, 13851];
        private static readonly int[] LeatherSquareCost = [2, 56, 224, 672, 2016, 4032, 6048, 9072, 13611, 20412];

        private static readonly Regex NonAlphaCharacters = new(@"[\d-]", RegexOptions.Compiled);

        private sealed class ArmorSlot
        {
            private readonly int[] _countsByExtension;

            public MaterialSize Size { get; }

            public ArmorSlot(MaterialSize size, params int[] countsByExtension)
            {
                Size = size;
                _countsByExtension = countsByExtension;
            }

            // Extension 0/1/2 map to their own entry; anything else (including 3+) uses the last entry.
            public int CountFor(int extension)
            {
                int lastIndex = _countsByExtension.Length - 1;
                return extension >= 0 && extension < lastIndex ? _countsByExtension[extension] : _countsByExtension[lastIndex];
            }
        }

        private sealed class ArmorTypeInfo
        {
            public MaterialType Type { get; set; }
            public Dictionary<int, ArmorSlot> Slots { get; set; }
        }

        private static readonly Dictionary<int, ArmorTypeInfo> ArmorTypeTable = new()
        {
            [ArmorObjectType.Cloth] = new()
            {
                Type = MaterialType.Cloth,
                Slots = new Dictionary<int, ArmorSlot>
                {
                    [EquipSlot.Head] = new(MaterialSize.Medium, 16, 24),
                    [EquipSlot.Hands] = new(MaterialSize.Small, 8, 8),
                    [EquipSlot.Feet] = new(MaterialSize.Small, 6, 8),
                    [EquipSlot.Torso] = new(MaterialSize.Large, 33, 40),
                    [EquipSlot.SubCloak] = new(MaterialSize.Small, 4, 22),
                    [EquipSlot.Legs] = new(MaterialSize.Large, 20, 24),
                    [EquipSlot.Arms] = new(MaterialSize.Medium, 13, 16),
                },
            },
            [ArmorObjectType.Leather] = new()
            {
                Type = MaterialType.Leather,
                Slots = new Dictionary<int, ArmorSlot>
                {
                    [EquipSlot.Head] = new(MaterialSize.Medium, 9, 12, 25, 30),
                    [EquipSlot.Hands] = new(MaterialSize.Small, 3, 4, 8, 10),
                    [EquipSlot.Feet] = new(MaterialSize.Small, 3, 4, 8, 10),
                    [EquipSlot.Torso] = new(MaterialSize.Large, 15, 21, 42, 50),
                    [EquipSlot.Legs] = new(MaterialSize.Large, 9, 12, 25, 30),
                    [EquipSlot.Arms] = new(MaterialSize.Medium, 6, 8, 16, 20),
                },
            },
            [ArmorObjectType.Studded] = new()
            {
                Type = MaterialType.Strips,
                Slots = new Dictionary<int, ArmorSlot>
                {
                    [EquipSlot.Head] = new(MaterialSize.Medium, 8, 8, 16, 19),
                    [EquipSlot.Hands] = new(MaterialSize.Small, 1, 2, 5, 6),
                    [EquipSlot.Feet] = new(MaterialSize.Small, 1, 2, 5, 6),
                    [EquipSlot.Torso] = new(MaterialSize.Large, 9, 13, 27, 32),
                    [EquipSlot.Legs] = new(MaterialSize.Large, 4, 8, 16, 19),
                    [EquipSlot.Arms] = new(MaterialSize.Medium, 3, 5, 10, 12),
                },
            },
            [ArmorObjectType.Chain] = new()
            {
                Type = MaterialType.Metal,
                Slots = new Dictionary<int, ArmorSlot>
                {
                    [EquipSlot.Head] = new(MaterialSize.Medium, 11, 20, 34, 41),
                    [EquipSlot.Hands] = new(MaterialSize.Small, 3, 6, 11, 13),
                    [EquipSlot.Feet] = new(MaterialSize.Small, 3, 6, 11, 13),
                    [EquipSlot.Torso] = new(MaterialSize.Large, 19, 33, 57, 69),
                    [EquipSlot.Legs] = new(MaterialSize.Large, 11, 20, 34, 41),
                    [EquipSlot.Arms] = new(MaterialSize.Medium, 7, 13, 23, 27),
                },
            },
            [ArmorObjectType.Plate] = new()
            {
                Type = MaterialType.Metal,
                Slots = new Dictionary<int, ArmorSlot>
                {
                    [EquipSlot.Head] = new(MaterialSize.Medium, 17, 27, 38, 38),
                    [EquipSlot.Hands] = new(MaterialSize.Small, 6, 9, 14, 13),
                    [EquipSlot.Feet] = new(MaterialSize.Small, 6, 9, 14, 13),
                    [EquipSlot.Torso] = new(MaterialSize.Large, 27, 45, 64, 66),
                    [EquipSlot.Legs] = new(MaterialSize.Large, 17, 27, 38, 38),
                    [EquipSlot.Arms] = new(MaterialSize.Medium, 11, 18, 26, 26),
                },
            },
            [ArmorObjectType.Reinforced] = new()
            {
                Type = MaterialType.Strips,
                Slots = new Dictionary<int, ArmorSlot>
                {
                    [EquipSlot.Head] = new(MaterialSize.Medium, 5, 8, 16, 19),
                    [EquipSlot.Hands] = new(MaterialSize.Small, 1, 2, 5, 6),
                    [EquipSlot.Feet] = new(MaterialSize.Small, 1, 2, 5, 6),
                    [EquipSlot.Torso] = new(MaterialSize.Large, 9, 13, 32, 32),
                    [EquipSlot.Legs] = new(MaterialSize.Large, 5, 8, 16, 19),
                    [EquipSlot.Arms] = new(MaterialSize.Medium, 3, 5, 10, 12),
                },
            },
            [ArmorObjectType.Scale] = new()
            {
                Type = MaterialType.Metal,
                Slots = new Dictionary<int, ArmorSlot>
                {
                    [EquipSlot.Head] = new(MaterialSize.Medium, 11, 20, 34, 41),
                    [EquipSlot.Hands] = new(MaterialSize.Small, 3, 6, 11, 13),
                    [EquipSlot.Feet] = new(MaterialSize.Small, 3, 6, 11, 13),
                    [EquipSlot.Torso] = new(MaterialSize.Large, 19, 33, 57, 69),
                    [EquipSlot.Legs] = new(MaterialSize.Large, 11, 20, 34, 41),
                    [EquipSlot.Arms] = new(MaterialSize.Medium, 7, 13, 23, 27),
                },
            },
        };

        private readonly struct WeaponYield
        {
            public readonly MaterialType Type;
            public readonly MaterialSize Size;
            public readonly int Count;

            public WeaponYield(MaterialType type, MaterialSize size, int count)
            {
                Type = type;
                Size = size;
                Count = count;
            }
        }

        private delegate WeaponYield WeaponRule(DbInventoryItem item, int subTier, eRealm realm);

        private static readonly Dictionary<int, WeaponRule> WeaponRules = new()
        {
            [2] = CrushingWeapon,
            [3] = SlashingWeapon,
            [4] = ThrustingWeapon,
            [5] = FiredWeapon,
            [6] = TwoHandedWeapon,
            [7] = PolearmWeapon,
            [8] = StaffWeapon,
            [9] = LongBowWeapon,
            [10] = CrossbowWeapon,
            [11] = MidgardSwordWeapon,
            [12] = HammerWeapon,
            [13] = AxeWeapon,
            [14] = SpearWeapon,
            [15] = CompositeBowWeapon,
            [16] = ThrowingWeapon,
            [17] = LeftAxeWeapon,
            [18] = RecurveBowWeapon,
            [19] = BladesWeapon,
            [20] = BluntWeapon,
            [21] = PiercingWeapon,
            [22] = LargeWeapon,
            [23] = CelticSpearWeapon,
            [24] = FlexibleWeapon,
            [25] = HandToHandWeapon,
            [26] = ScytheWeapon,
            [27] = FistwrapWeapon,
            [28] = StaffWeapon, // Mauler staff copies the caster-staff salvage rates.
            [42] = ShieldEquipment,
            [45] = InstrumentEquipment,
        };

        private static WeaponYield CrushingWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 7,   // hammer
                2 => 11,  // mace
                3 => 14,  // flanged mace
                4 => 16,  // spiked mace
                51 => 26, // exceptional/fortified tier
                _ => 21,  // war hammer
            };
            return new(MaterialType.Metal, MaterialSize.Medium, count);
        }

        private static WeaponYield SlashingWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => item.SPD_ABS <= 24 ? 6 : 7,   // dagger vs short sword
                2 => item.SPD_ABS <= 27 ? 10 : 14, // handaxe vs broadsword
                3 => 15,                           // scimitar
                4 => 19,                           // long sword
                51 => 27,                          // exceptional/fortified tier
                _ => item.SPD_ABS <= 36 ? 27 : 23, // sabre vs bastard sword
            };
            return new(MaterialType.Metal, MaterialSize.Medium, count);
        }

        private static WeaponYield ThrustingWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 6,   // dirk
                2 => 9,   // stiletto
                3 => 14,  // main gauche
                4 => 16,  // rapier
                51 => 26, // exceptional/fortified tier
                _ => 21,  // gladius
            };
            return new(MaterialType.Metal, MaterialSize.Medium, count);
        }

        private static WeaponYield FiredWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 15,
                2 => 15,
                3 => 15, // Hibernia short bow
                4 => 15,
                51 => 8, // heavy/exceptional/fortified short bow
                _ => 12, // hunting bow
            };
            return new(MaterialType.Wood, MaterialSize.Small, count);
        }

        private static WeaponYield TwoHandedWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 11,                           // war mattock
                2 => item.SPD_ABS <= 44 ? 20 : 14, // war axe vs two-handed sword
                3 => 18,                           // great hammer
                4 => item.SPD_ABS <= 45 ? 21 : 24, // battle axe vs great sword
                51 => 32,                          // exceptional/fortified tier
                _ => 27,                           // great axe
            };
            return new(MaterialType.Metal, MaterialSize.Large, count);
        }

        private static WeaponYield PolearmWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 10,  // pike
                2 => 12,  // lochaber axe
                3 => 15,  // bill
                4 => 23,  // lucerne hammer
                51 => 30, // exceptional/fortified tier
                _ => 25,  // halberd
            };
            return new(MaterialType.Metal, MaterialSize.Large, count);
        }

        private static WeaponYield StaffWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => item.SPD_ABS <= 30 ? 16 : 24, // quarterstaff vs plain staff
                2 => 16,
                3 => realm switch
                {
                    eRealm.Albion => item.SPD_ABS <= 42 ? 26 : 22, // shod quarterstaff vs shod staff
                    _ => 24,                                       // Midgard/Hibernia shod staff
                },
                4 => 24,
                51 => 39, // heavy shod staff / exceptional quarterstaff / exceptional magus staff
                _ => 24,
            };
            return new(MaterialType.Wood, MaterialSize.Medium, count);
        }

        private static WeaponYield LongBowWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            if (subTier == 51)
                return new(MaterialType.Wood, MaterialSize.Large, 39);

            int count = SpeedTieredCount(item.SPD_ABS, 40, 47, slow: 12, medium: 16, fast: 22); // hunting bow / bow / long bow
            return new(MaterialType.Wood, MaterialSize.Large, count);
        }

        private static WeaponYield CrossbowWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            if (subTier == 51)
                return new(MaterialType.Wood, MaterialSize.Medium, 22);

            int count = SpeedTieredCount(item.SPD_ABS, 40, 47, slow: 12, medium: 16, fast: 22);
            return new(MaterialType.Wood, MaterialSize.Medium, count);
        }

        private static WeaponYield MidgardSwordWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 7,   // dagger
                2 => 10,  // short sword
                3 => 14,  // broadsword
                4 => 19,  // long sword
                51 => 28, // dwarven short sword
                _ => 23,  // bastard sword
            };
            return new(MaterialType.Metal, MaterialSize.Large, count);
        }

        private static WeaponYield HammerWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 7,   // small hammer
                2 => 12,  // hammer
                3 => 15,  // war hammer
                4 => 20,  // pick hammer
                51 => 28, // spiked hammer
                _ => 25,  // battle hammer
            };
            return new(MaterialType.Metal, MaterialSize.Large, count);
        }

        private static WeaponYield AxeWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 7,   // hand axe
                2 => 11,  // bearded axe
                3 => 15,  // war axe
                4 => 19,  // spiked axe
                51 => 27, // cleaver
                _ => 23,  // double-bladed axe
            };
            return new(MaterialType.Metal, MaterialSize.Medium, count);
        }

        private static WeaponYield SpearWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 10,  // spear
                2 => 12,  // long spear
                3 => 15,  // trident
                4 => 20,  // lugged spear
                51 => 32, // battle spear
                _ => 27,  // great spear
            };
            return new(MaterialType.Metal, MaterialSize.Large, count);
        }

        private static WeaponYield CompositeBowWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            if (subTier == 51)
                return new(MaterialType.Wood, MaterialSize.Large, 28);

            int count = item.SPD_ABS <= 40 ? 12 : 16;
            return new(MaterialType.Wood, MaterialSize.Large, count);
        }

        // Only costs one metal bar to craft, so it never yields anything back.
        private static WeaponYield ThrowingWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            return new(MaterialType.Metal, MaterialSize.None, 0);
        }

        private static WeaponYield LeftAxeWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            return new(MaterialType.Metal, MaterialSize.None, item.Level < 20 ? 0 : 1);
        }

        private static WeaponYield RecurveBowWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            if (subTier == 51)
                return new(MaterialType.Wood, MaterialSize.Medium, 39);

            int count = SpeedTieredCount(item.SPD_ABS, 40, 47, slow: 12, medium: 16, fast: 22);
            return new(MaterialType.Wood, MaterialSize.Medium, count);
        }

        private static WeaponYield BladesWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 7,   // short sword
                2 => 11,  // falcata
                3 => 14,  // broadsword
                4 => 19,  // long sword
                51 => 27, // exceptional/fortified tier
                _ => 23,  // bastard sword
            };
            return new(MaterialType.Metal, MaterialSize.Medium, count);
        }

        private static WeaponYield BluntWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 7,   // club
                2 => 11,  // mace
                3 => 14,  // spiked club
                4 => 16,  // hammer
                51 => 26, // exceptional/fortified tier
                _ => 21,  // spiked mace
            };
            return new(MaterialType.Metal, MaterialSize.Medium, count);
        }

        private static WeaponYield PiercingWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 6,   // dirk
                2 => 9,   // dagger
                3 => 14,  // stiletto
                4 => 16,  // rapier
                51 => 26, // exceptional/fortified tier
                _ => 21,  // curved dagger
            };
            return new(MaterialType.Metal, MaterialSize.Medium, count);
        }

        private static WeaponYield LargeWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            if (subTier == 3)
                return new(MaterialType.Metal, MaterialSize.Large, item.SPD_ABS <= 50 ? 23 : 24);

            if (subTier == 51)
                return new(MaterialType.Metal, MaterialSize.Large, 32);

            int count = SpeedTieredCount(item.SPD_ABS, 46, 47, slow: 14, medium: 18, fast: 27); // big shillelagh-type weapon / - / great sword
            return new(MaterialType.Metal, MaterialSize.Large, count);
        }

        private static WeaponYield CelticSpearWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 7,   // short spear
                2 => 11,  // spear
                3 => 16,  // long spear
                4 => 19,  // war spear
                51 => 32, // exceptional battle spear / hooked spear
                _ => 21,  // barbed spear
            };
            return new(MaterialType.Metal, MaterialSize.Large, count);
        }

        private static WeaponYield FlexibleWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 9,   // chain
                2 => 14,  // morning star / tipped whip
                3 => 16,  // war chain / blade-tipped whip
                51 => 23, // exceptional/fortified tier
                _ => 19,  // flail / barbed whip
            };
            return new(MaterialType.Metal, MaterialSize.Medium, count);
        }

        private static WeaponYield HandToHandWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 9,   // moon fang
                2 => 11,  // moon claw
                3 => 14,  // bladed moon fang
                51 => 20, // heavy bladed claw greave
                _ => 14,  // bladed moon claw
            };
            return new(MaterialType.Metal, MaterialSize.Small, count);
        }

        private static WeaponYield ScytheWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            int count = subTier switch
            {
                1 => 10,  // scythe
                2 => 12,  // harvest scythe
                3 => 15,  // martial scythe
                4 => 20,  // war scythe
                51 => 32, // moon-crested war scythe
                _ => 27,  // great war scythe
            };
            return new(MaterialType.Metal, MaterialSize.Large, count);
        }

        // Mauler fistwraps: no salvage data was ever recorded for these.
        private static WeaponYield FistwrapWeapon(DbInventoryItem item, int subTier, eRealm realm)
        {
            return new(MaterialType.Metal, MaterialSize.Small, 0);
        }

        private static WeaponYield ShieldEquipment(DbInventoryItem item, int subTier, eRealm realm)
        {
            return item.Type_Damage switch
            {
                1 => new(MaterialType.Metal, MaterialSize.Small, subTier == 51 ? 3 : 2), // buckler/round shield
                2 => new(MaterialType.Metal, MaterialSize.Medium, 3),                    // heater shield
                _ => new(MaterialType.Metal, MaterialSize.Large, 4),                     // block/tower shield
            };
        }

        private static WeaponYield InstrumentEquipment(DbInventoryItem item, int subTier, eRealm realm)
        {
            return new(MaterialType.Wood, MaterialSize.Medium, 18); // drum, lute, flute, harp
        }

        public static SalvageReturn GetSalvage(GamePlayer Player, DbInventoryItem Item)
        {
            SalvageReturn yield = new()
            {
                ID = string.Empty,
                Count = 0,
                Level = Item.Level,
                Tier = ComputeTier(Item.Level),
            };

            int subTier = ComputeSubTier(Item.Level, yield.Tier);
            int extension = Item.SalvageExtension;

            // Cloaks are a special case: an unrecognized cloak sub-type salvages
            // nothing at all and skips every calculation below.
            if (Item.Object_Type is ArmorObjectType.Cloak)
            {
                if (!TryGetCloakYield(extension, Item.Item_Type, out MaterialType cloakType, out MaterialSize cloakSize, out var cloakCount))
                    return yield;

                FinishYield(Player, Item, cloakType, cloakSize, cloakCount, ref yield);
                return yield;
            }

            MaterialType materialType = MaterialType.Cloth;
            MaterialSize materialSize = MaterialSize.None;
            int rawCount = 0;

            if (ArmorTypeTable.TryGetValue(Item.Object_Type, out ArmorTypeInfo armorInfo))
            {
                materialType = armorInfo.Type;

                if (armorInfo.Slots.TryGetValue(Item.Item_Type, out ArmorSlot slot))
                {
                    materialSize = slot.Size;
                    rawCount = slot.CountFor(extension);
                }
            }
            else if (WeaponRules.TryGetValue(Item.Object_Type, out WeaponRule weaponRule))
            {
                WeaponYield weaponYield = weaponRule(Item, subTier, Player.Realm);
                materialType = weaponYield.Type;
                materialSize = weaponYield.Size;
                rawCount = weaponYield.Count;
            }

            FinishYield(Player, Item, materialType, materialSize, rawCount, ref yield);
            return yield;
        }

        private static bool TryGetCloakYield(int extension, int itemType, out MaterialType type, out MaterialSize size, out int count)
        {
            switch (itemType)
            {
                case EquipSlot.SubCloak: // Cloak/Hooded Cloak (ext 0) vs Dressy/Fancy/Regal/Fine Cloak (ext 1+)
                    type = MaterialType.Cloth;
                    size = MaterialSize.Small;
                    count = extension == 0 ? 4 : 22;
                    return true;
                case 24:
                case 29:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                    type = MaterialType.Jewel;
                    size = MaterialSize.None;
                    count = 1;
                    return true;
                default:
                    type = default;
                    size = default;
                    count = 0;
                    return false;
            }
        }

        private static void FinishYield(GamePlayer player, DbInventoryItem item, MaterialType type, MaterialSize size, int rawCount, ref SalvageReturn yield)
        {
            // Drops (not player-crafted) below level 49 are penalized one tier down
            // and cost/price is looked up one tier higher than the reduced tier.

            bool isDropPenalized = !item.IsCrafted && yield.Tier > 0 && item.Level < 49;

            if (isDropPenalized)
                yield.Tier--;

            ApplyMaterialYield(item, player.Realm, type, size, rawCount, isDropPenalized, ref yield);

            yield.Skill = yield.Tier * 100 + 15;
            yield.Material = CleanMaterialName(yield.ID);

            ApplyPriceRules(player, item, ref yield);

            // Special case for Atlas ROG items: they always yield 2 salvage items regardless of other calculations.
            if (!string.IsNullOrEmpty(item.Description) && item.Description.Contains("Atlas ROG"))
                yield.Count = 2;
            else
                yield.Count = (int) (yield.Count * Properties.SALVAGE_YIELD_MULTIPLIER);
        }

        private static void ApplyMaterialYield(DbInventoryItem item, eRealm realm, MaterialType type, MaterialSize size, int rawCount, bool isDropPenalized, ref SalvageReturn yield)
        {
            int realmOffset = RealmOffset(realm);
            int count = Math.Max(rawCount, 0);
            int costTier = isDropPenalized ? yield.Tier + 1 : yield.Tier;

            switch (type)
            {
                case MaterialType.Cloth:
                    yield.ID = ClothSalvage[realmOffset + yield.Tier];
                    yield.MSRP = ClothSquareCost[costTier] * count * 2;
                    count = item.IsCrafted ? count : RawMaterialReturn(size, small: 18, medium: 36, large: 54);
                    break;
                case MaterialType.Leather:
                    yield.ID = LeatherSalvage[realmOffset + yield.Tier];
                    yield.MSRP = LeatherSquareCost[costTier] * count * 2;
                    count = item.IsCrafted ? count : RawMaterialReturn(size, small: 18, medium: 36, large: 54);
                    break;
                case MaterialType.Strips:
                    yield.ID = StripsSalvage[realmOffset + yield.Tier];
                    yield.MSRP = MetalBarCost[costTier] * count * 2;
                    count = item.IsCrafted ? count : RawMaterialReturn(size, small: 5, medium: 10, large: 15);
                    break;
                case MaterialType.Wood:
                    yield.ID = WoodSalvage[realmOffset + yield.Tier];
                    yield.MSRP = WoodPlankCost[costTier] * count * 2;
                    count = item.IsCrafted ? count : RawMaterialReturn(size, small: 12, medium: 24, large: 36);
                    break;
                case MaterialType.Metal:
                    yield.ID = MetalSalvage[realmOffset + yield.Tier];
                    yield.MSRP = MetalBarCost[costTier] * count * 2;
                    count = item.IsCrafted ? count : RawMaterialReturn(size, small: 5, medium: 10, large: 15);
                    break;
                case MaterialType.Jewel:
                    yield.ID = JewelSalvage[yield.Tier];
                    count = 1;
                    yield.MSRP = JewelCost[costTier] * count * 2;
                    break;
                default:
                    // Not reachable through the tables above.
                    yield.MSRP = MetalBarCost[costTier] * count * 2;
                    yield.ID = ClothSalvage[0];
                    count = 0;
                    break;
            }

            yield.Count = count;
        }

        private static int SpeedTieredCount(int speed, int lowThreshold, int highThreshold, int slow, int medium, int fast)
        {
            return speed <= lowThreshold ? slow : speed <= highThreshold ? medium : fast;
        }

        private static int RawMaterialReturn(MaterialSize size, int small, int medium, int large)
        {
            return size switch
            {
                MaterialSize.Small => small,
                MaterialSize.Medium => medium,
                MaterialSize.Large => large,
                _ => 0,
            };
        }

        private static int RealmOffset(eRealm realm)
        {
            return realm switch
            {
                eRealm.Albion => 0,
                eRealm.Midgard => 10,
                eRealm.Hibernia => 20,
                _ => throw new ArgumentOutOfRangeException(nameof(realm), $"Unexpected realm value: {realm}"),
            };
        }

        private static int ComputeTier(int itemLevel)
        {
            int tier = itemLevel * 2 / 10;
            return Math.Min(tier, 9);
        }

        private static int ComputeSubTier(int itemLevel, int tier)
        {
            return itemLevel > 50 ? 51 : itemLevel - tier * 5;
        }

        private static string CleanMaterialName(string id)
        {
            string spaced = id.Replace('_', ' ');
            return NonAlphaCharacters.Replace(spaced, string.Empty);
        }

        private static void ApplyPriceRules(GamePlayer player, DbInventoryItem item, ref SalvageReturn yield)
        {
            bool isPrivileged = player.Client.Account.PrivLevel != 1;

            if (item.Price == 0)
            {
                if (isPrivileged)
                {
                    player.Out.SendDebugMessage(
                        $"Items with a price set to 0 cannot be salvaged or sold to merchants, the MSRP for this item is {yield.MSRP}c Use /setsalvage if return value needs changed");
                }

                yield.Count = 0;
                return;
            }

            if (isPrivileged)
            {
                player.Out.SendDebugMessage(
                    $"CALCULATOR: ObjectType {item.Object_Type} ItemType {item.Item_Type} Count {yield.Count} Material {yield.Material} Item.Price {item.Price} MSRP {yield.MSRP}");
            }
        }
    }
}
