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
 */
using System;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS
{

    /// <summary>
    /// all known slots
    /// </summary>
    public class Slot
	{
		public const int HORSEARMOR = 7;
		public const int HORSEBARDING = 8;
		public const int HORSE = 9;
		public const int RIGHTHAND = 10;
		public const int LEFTHAND = 11;
		public const int TWOHAND = 12;
		public const int RANGED = 13;
		public const int FIRSTQUIVER = 14;
		public const int SECONDQUIVER = 15;
		public const int THIRDQUIVER = 16;
		public const int FOURTHQUIVER = 17;
		public const int HELM = 21;
		public const int HANDS = 22;
		public const int FEET = 23;
		public const int JEWELRY = 24;
		public const int TORSO = 25;
		public const int CLOAK = 26;
		public const int LEGS = 27;
		public const int ARMS = 28;
		public const int NECK = 29;
		public const int FOREARMS = 30;
		public const int SHIELD = 31;
		public const int WAIST = 32;
		public const int LEFTWRIST = 33;
		public const int RIGHTWRIST = 34;
		public const int LEFTRING = 35;
		public const int RIGHTRING = 36;
		public const int MYTHICAL = 37;
	};

	/// <summary>
	/// The armor ability level for different armor types
	/// </summary>
	public abstract class ArmorLevel
	{
		public const int GenericArmor = 0;
		public const int Cloth = 1;
		public const int Leather = 2;
		public const int Reinforced = 3;
		public const int Studded = 3;
		public const int Scale = 4;
		public const int Chain = 4;
		public const int Plate = 5;
	}

	/// <summary>
	/// Ressurection sickness types
	/// </summary>
	public enum eResurectionSicknessType : int
	{
		PvMSickness = 0,
		RvRSickness = 1,
		NoSickness = 2
	}

	/// <summary>
	/// the size of a shield
	/// </summary>
	public abstract class ShieldLevel
	{
		public const int Small = 1;
		public const int Medium = 2;
		public const int Large = 3;
	}

	/// <summary>
	/// The type of instrument
	/// </summary>
	public abstract class InstrumentType
	{
		public const int Drum = 1;
		public const int Lute = 2;
		public const int Flute = 3;
		public const int Harp = 4;
	}

	/// <summary>
	/// strong name constants of spell line used in the world (poison, proc ect ...)
	/// </summary>
	public abstract class GlobalSpellsLines
	{
		public const string Combat_Styles_Effect = "Combat Style Effects";
		public const string Mundane_Poisons = "Mundane Poisons";
		public const string Reserved_Spells = "Reserved Spells"; // Masterlevels
		public const string SiegeWeapon_Spells = "SiegeWeapon Spells";
		public const string Item_Effects = "Item Effects";
		public const string Potions_Effects = "Potions";
		public const string Mob_Spells = "Mob Spells";
		public const string Character_Abilities = "Character Abilities"; // dirty tricks, flurry ect...
		public const string Item_Spells = "Item Spells";	// Combine scroll etc.
		public const string Champion_Lines_StartWith = "Champion ";
        public const string Realm_Spells = "Realm Spells"; // Resurrection illness, Speed of the realm
    }

	public static class GlobalConstants
	{
		private static readonly Dictionary<EAttackResult, byte> AttackResultByte = new Dictionary<EAttackResult, byte>()
	    {
			{EAttackResult.Missed, 0},
			{EAttackResult.Parried, 1},
			{EAttackResult.Blocked, 2},
			{EAttackResult.Evaded, 3},
			{EAttackResult.Fumbled, 4},
			{EAttackResult.HitUnstyled, 10},
			{EAttackResult.HitStyle, 11},
			{EAttackResult.Any, 20},
	    };
		
		public static byte GetAttackResultByte(EAttackResult attResult)
		{
			if (AttackResultByte.ContainsKey(attResult))
			{
				return AttackResultByte[attResult];
			}
			
			return 0;
		}
		
		public static bool IsExpansionEnabled(int expansion)
		{
			bool enabled = true;
			foreach (string ex in Util.SplitCSV(ServerProperties.Properties.DISABLED_EXPANSIONS, true))
			{
				int exNum = 0;
				if (int.TryParse(ex, out exNum))
				{
					if (exNum == expansion)
					{
						enabled = false;
						break;
					}
				}
			}

			return enabled;
		}


		public static string StatToName(EStat stat)
		{
			switch (stat)
			{
				case EStat.STR:
					return "Strength";
				case EStat.DEX:
					return "Dexterity";
				case EStat.CON:
					return "Constitution";
				case EStat.QUI:
					return "Quickness";
				case EStat.INT:
					return "Intelligence";
				case EStat.PIE:
					return "Piety";
				case EStat.EMP:
					return "Empathy";
				case EStat.CHR:
					return "Charisma";
			}

			return "Unknown";
		}

		/// <summary>
		/// Check an Object_Type to determine if it's a Bow weapon
		/// </summary>
		/// <param name="objectType"></param>
		/// <returns></returns>
		public static bool IsBowWeapon(EObjectType objectType)
		{
			return (objectType == EObjectType.CompositeBow || objectType == EObjectType.Longbow || objectType == EObjectType.RecurvedBow);
		}
		/// <summary>
		/// Check an Object_Type to determine if it's a weapon
		/// </summary>
		/// <param name="objectTypeID"></param>
		/// <returns></returns>
		public static bool IsWeapon(int objectTypeID)
		{
			if ((objectTypeID >= 1 && objectTypeID <= 28) || objectTypeID == (int)EObjectType.Shield) return true;
			return false;
		}
		/// <summary>
		/// Check an Object_Type to determine if it's armor
		/// </summary>
		/// <param name="objectTypeID"></param>
		/// <returns></returns>
		public static bool IsArmor(int objectTypeID)
		{
			if (objectTypeID >= 32 && objectTypeID <= 38) return true;
			return false;
		}
		/// <summary>
		/// Offensive, Defensive, or Positional
		/// </summary>
		public static string StyleOpeningTypeToName(int openingType)
		{
			return Enum.GetName(typeof(Styles.Style.eOpening), openingType);
		}
		/// <summary>
		/// Position, Back, Side, Front
		/// </summary>
		public static string StyleOpeningPositionToName(int openingRequirement)
		{
			return Enum.GetName(typeof(Styles.Style.eOpeningPosition), openingRequirement);
		}
		/// <summary>
		/// Attack Result. Any, Miss, Hit, Parry, Block, Evade, Fumble, Style.
		/// </summary>
		public static string StyleAttackResultToName(int attackResult)
		{
			return Enum.GetName(typeof(Styles.Style.eAttackResultRequirement), attackResult);
		}

		public static string InstrumentTypeToName(int instrumentTypeID)
		{
			return Enum.GetName(typeof(EInstrumentType), instrumentTypeID);
		}

		public static string AmmunitionTypeToDamageName(int ammutype)
		{
			ammutype &= 0x3;
			switch (ammutype)
			{
					case 1: return "medium";
					case 2: return "heavy";
					case 3: return "X-heavy";
			}
			return "light";
		}

		public static string AmmunitionTypeToRangeName(int ammutype)
		{
			ammutype = (ammutype >> 2) & 0x3;
			switch (ammutype)
			{
					case 1: return "medium";
					case 2: return "long";
					case 3: return "X-long";
			}
			return "short";
		}

		public static string AmmunitionTypeToAccuracyName(int ammutype)
		{
			ammutype = (ammutype >> 4) & 0x3;
			switch (ammutype)
			{
					case 1: return "normal";
					case 2: return "improved";
					case 3: return "enhanced";
			}
			return "reduced";
		}

		public static string ShieldTypeToName(int shieldTypeID)
		{
			return Enum.GetName(typeof(ShieldLevel), shieldTypeID);
		}

		public static string ArmorLevelToName(int armorLevel, ERealm realm)
		{
			switch (realm)
			{
				case ERealm.Albion:
					{
						switch (armorLevel)
						{
								case ArmorLevel.Cloth: return "cloth";
								case ArmorLevel.Chain: return "chain";
								case ArmorLevel.Leather: return "leather";
								case ArmorLevel.Plate: return "plate";
								case ArmorLevel.Studded: return "studded";
								default: return "undefined";
						}
					}
				case ERealm.Midgard:
					{
						switch (armorLevel)
						{
								case ArmorLevel.Cloth: return "cloth";
								case ArmorLevel.Chain: return "chain";
								case ArmorLevel.Leather: return "leather";
								case ArmorLevel.Studded: return "studded";
								default: return "undefined";
						}
					}
				case ERealm.Hibernia:
					{
						switch (armorLevel)
						{
								case ArmorLevel.Cloth: return "cloth";
								case ArmorLevel.Scale: return "scale";
								case ArmorLevel.Leather: return "leather";
								case ArmorLevel.Reinforced: return "reinforced";
								default: return "undefined";
						}
					}
					default: return "undefined";
			}
		}

		public static string WeaponDamageTypeToName(int weaponDamageTypeID)
		{
			return Enum.GetName(typeof(EWeaponDamageType), weaponDamageTypeID);
		}

		public static string NameToShortName(string name)
		{
			string[] values = name.Trim().ToLower().Split(' ');
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].Length == 0) continue;
				if (i > 0 && values[i] == "of")
					return values[i - 1];
			}
			return values[values.Length - 1];
		}

		public static string ItemHandToName(int handFlag)
		{
			if (handFlag == 1) return "twohanded";
			if (handFlag == 2) return "lefthand";
			return "both";
		}

		public static string ObjectTypeToName(int objectTypeID)
		{
			switch (objectTypeID)
			{
					case 0: return "generic (item)";
					case 1: return "generic (weapon)";
					case 2: return "crushing (weapon)";
					case 3: return "slashing (weapon)";
					case 4: return "thrusting (weapon)";
					case 5: return "fired (weapon)";
					case 6: return "twohanded (weapon)";
					case 7: return "polearm (weapon)";
					case 8: return "staff (weapon)";
					case 9: return "longbow (weapon)";
					case 10: return "crossbow (weapon)";
					case 11: return "sword (weapon)";
					case 12: return "hammer (weapon)";
					case 13: return "axe (weapon)";
					case 14: return "spear (weapon)";
					case 15: return "composite bow (weapon)";
					case 16: return "thrown (weapon)";
					case 17: return "left axe (weapon)";
					case 18: return "recurve bow (weapon)";
					case 19: return "blades (weapon)";
					case 20: return "blunt (weapon)";
					case 21: return "piercing (weapon)";
					case 22: return "large (weapon)";
					case 23: return "celtic spear (weapon)";
					case 24: return "flexible (weapon)";
					case 25: return "hand to hand (weapon)";
					case 26: return "scythe (weapon)";
					case 27: return "fist wraps (weapon)";
					case 28: return "mauler staff (weapon)";
					case 31: return "generic (armor)";
					case 32: return "cloth (armor)";
					case 33: return "leather (armor)";
					case 34: return "studded leather (armor)";
					case 35: return "chain (armor)";
					case 36: return "plate (armor)";
					case 37: return "reinforced (armor)";
					case 38: return "scale (armor)";
					case 41: return "magical (item)";
					case 42: return "shield (armor)";
					case 43: return "arrow (item)";
					case 44: return "bolt (item)";
					case 45: return "instrument (item)";
					case 46: return "poison (item)";
					case 47: return "alchemy tincture";
					case 48: return "spellcrafting gem";
					case 49: return "garden object";
					case 50: return "house wall object";
					case 51: return "house floor object";
					case 53: return "house npc";
					case 54: return "house vault";
					case 55: return "house crafting object";
					case 68: return "house bindstone";
			}
			return "unknown (item)";
		}

		//This method translates an InventoryTypeID to a string
		public static string SlotToName(int slotID)
		{
			switch (slotID)
			{
				case 0x0A: return "righthand";
				case 0x0B: return "lefthand";
				case 0x0C: return "twohanded";
				case 0x0D: return "distance";
				case 0x15: return "head";
				case 0x16: return "hand";
				case 0x17: return "feet";
				case 0x18: return "jewel";
				case 0x19: return "torso";
				case 0x1A: return "cloak";
				case 0x1B: return "legs";
				case 0x1C: return "arms";
				case 0x1D: return "neck";
				case 0x20: return "belt";
				case 0x21: return "leftbracer";
				case 0x22: return "rightbracer";
				case 0x23: return "leftring";
				case 0x24: return "rightring";
				case 0x25: return "mythirian";
				case 96: return "leftfront saddlebag";
				case 97: return "rightfront saddlebag";
				case 98: return "leftrear saddlebag";
				case 99: return "rightrear saddlebag";
			}
			return "generic inventory";
		}

		//This method translates a string to an InventorySlotID
		public static byte NameToSlot(string name)
		{
			switch (name)
			{
					//Horses
					case "mount": return 0xA9;
					//Righthand Weapon Type
					case "righthand": return 0x0A;
					case "right": return 0x0A;
					case "ri": return 0x0A;

					//Lefthand Weapon Type
					case "lefthand": return 0x0B;
					case "left": return 0x0B;
					case "lef": return 0x0B;

					//Twohanded Weapon Type
					case "twohanded": return 0x0C;
					case "two": return 0x0C;
					case "tw": return 0x0C;

					//Distance Weapon Type
					case "distance": return 0x0D;
					case "dist": return 0x0D;
					case "di": return 0x0D;
					case "bow": return 0x0D;
					case "crossbow": return 0x0D;
					case "longbow": return 0x0D;
					case "throwing": return 0x0D;
					case "thrown": return 0x0D;
					case "fire": return 0x0D;
					case "firing": return 0x0D;

					//Head Armor Type
					case "head": return 0x15;
					case "helm": return 0x15;
					case "he": return 0x15;

					//Hand Armor Type
					case "hands": return 0x16;
					case "hand": return 0x16;
					case "ha": return 0x16;
					case "gloves": return 0x16;
					case "glove": return 0x16;
					case "gl": return 0x16;

					//Boot Armor Type
					case "boots": return 0x17;
					case "boot": return 0x17;
					case "boo": return 0x17;
					case "feet": return 0x17;
					case "fe": return 0x17;
					case "foot": return 0x17;
					case "fo": return 0x17;

					//Jewel Type
					case "jewels": return 0x18;
					case "jewel": return 0x18;
					case "je": return 0x18;
					case "j": return 0x18;
					case "gems": return 0x18;
					case "gem": return 0x18;
					case "gemstone": return 0x18;
					case "stone": return 0x18;

					//Body Armor Type
					case "torso": return 0x19;
					case "to": return 0x19;
					case "body": return 0x19;
					case "bod": return 0x19;
					case "robes": return 0x19;
					case "robe": return 0x19;
					case "ro": return 0x19;

					//Cloak Armor Type
					case "cloak": return 0x1A;
					case "cloa": return 0x1A;
					case "clo": return 0x1A;
					case "cl": return 0x1A;
					case "cape": return 0x1A;
					case "ca": return 0x1A;
					case "gown": return 0x1A;
					case "mantle": return 0x1A;
					case "ma": return 0x1A;
					case "shawl": return 0x1A;

					//Leg Armor Type
					case "legs": return 0x1B;
					case "leg": return 0x1B;

					//Arms Armor Type
					case "arms": return 0x1C;
					case "arm": return 0x1C;
					case "ar": return 0x1C;

					//Neck Armor Type
					case "neck": return 0x1D;
					case "ne": return 0x1D;
					case "scruff": return 0x1D;
					case "nape": return 0x1D;
					case "throat": return 0x1D;
					case "necklace": return 0x1D;
					case "necklet": return 0x1D;

					//Belt Armor Type
					case "belt": return 0x20;
					case "b": return 0x20;
					case "girdle": return 0x20;
					case "waistbelt": return 0x20;

					//Left Bracers Type
					case "leftbracers": return 0x21;
					case "leftbracer": return 0x21;
					case "leftbr": return 0x21;
					case "lbracers": return 0x21;
					case "lbracer": return 0x21;
					case "leb": return 0x21;
					case "lbr": return 0x21;
					case "lb": return 0x21;

					//Right Bracers Type
					case "rightbracers": return 0x22;
					case "rightbracer": return 0x22;
					case "rightbr": return 0x22;
					case "rbracers": return 0x22;
					case "rbracer": return 0x22;
					case "rib": return 0x22;
					case "rbr": return 0x22;
					case "rb": return 0x22;

					//Left Ring Type
					case "leftrings": return 0x23;
					case "leftring": return 0x23;
					case "leftr": return 0x23;
					case "lrings": return 0x23;
					case "lring": return 0x23;
					case "lri": return 0x23;
					case "ler": return 0x23;
					case "lr": return 0x23;

					//Right Ring Type
					case "rightrings": return 0x24;
					case "rightring": return 0x24;
					case "rightr": return 0x24;
					case "rrings": return 0x24;
					case "rring": return 0x24;
					case "rri": return 0x24;
					case "rir": return 0x24;
					case "rr": return 0x24;

					//Mythirians
					case "myth": return 0x25;
					case "mythirian": return 0x25;
					case "mythirians": return 0x25;
			}
			return 0x00;
		}
		public static string RealmToName(ERealm realm)
		{
			switch (realm)
			{
					case ERealm.None: return "None";
					case ERealm.Albion: return "Albion";
					case ERealm.Midgard: return "Midgard";
					case ERealm.Hibernia: return "Hibernia";
					case ERealm.Door: return "Door";
					default: return "";
			}
		}
		public static int EmblemOfRealm(ERealm realm)
		{
			switch (realm)
			{
					case ERealm.None: return 0;
					case ERealm.Albion: return 464;
					case ERealm.Midgard: return 465;
					case ERealm.Hibernia: return 466;
					default: return 0;
			}
		}

		public static string PropertyToName(EProperty property)
		{
			switch (property)
			{
					case EProperty.Strength: return "Strength";
					case EProperty.Dexterity: return "Dexterity";
					case EProperty.Constitution: return "Constitution";
					case EProperty.Quickness: return "Quickness";
					case EProperty.Intelligence: return "Intelligence";
					case EProperty.Piety: return "Piety";
					case EProperty.Empathy: return "Empathy";
					case EProperty.Charisma: return "Charisma";
					case EProperty.Resist_Body: return "Body Resist";
					case EProperty.Resist_Cold: return "Cold Resist";
					case EProperty.Resist_Crush: return "Crush Resist";
					case EProperty.Resist_Energy: return "Energy Resist";
					case EProperty.Resist_Heat: return "Heat Resist";
					case EProperty.Resist_Matter: return "Matter Resist";
					case EProperty.Resist_Slash: return "Slash Resist";
					case EProperty.Resist_Spirit: return "Spirit Resist";
					case EProperty.Resist_Thrust: return "Thrust Resist";
					case EProperty.Resist_Natural: return "Essence Resist";
					default: return "not implemented";
			}
		}

		public static string DamageTypeToName(EDamageType damage)
		{
			switch (damage)
			{
					case EDamageType.Body: return "Body";
					case EDamageType.Cold: return "Cold";
					case EDamageType.Crush: return "Crush";
					case EDamageType.Energy: return "Energy";
					case EDamageType.Falling: return "Falling";
					case EDamageType.Heat: return "Heat";
					case EDamageType.Matter: return "Matter";
					case EDamageType.Natural: return "Natural";
					case EDamageType.Slash: return "Slash";
					case EDamageType.Spirit: return "Spirit";
					case EDamageType.Thrust: return "Thrust";
					default: return "unknown damagetype " + damage.ToString();
			}
		}

		public static string CraftLevelToCraftTitle(GameClient client, int craftLevel)
		{
			switch ((int)(craftLevel / 100))
			{
                case 0: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Helper");
                case 1: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.JuniorApprentice");
                case 2: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Apprentice");
                case 3: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Neophyte");
                case 4: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Assistant");
                case 5: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Junior");
                case 6: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Journeyman");
                case 7: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Senior");
                case 8: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Master");
                case 9: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Grandmaster");
                case 10: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.Legendary");
                case 11: return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.LegendaryGrandmaster");
			}
			if (craftLevel > 1100)
			{
                return LanguageMgr.GetTranslation(client.Account.Language, "CraftLevelToCraftTitle.LegendaryGrandmaster");
			}
			return "";
		}

		public static ERealm GetBonusRealm(EProperty bonus)
		{
			if (SkillBase.CheckPropertyType(bonus, EPropertyType.Albion))
				return ERealm.Albion;
			if (SkillBase.CheckPropertyType(bonus, EPropertyType.Midgard))
				return ERealm.Midgard;
			if (SkillBase.CheckPropertyType(bonus, EPropertyType.Hibernia))
				return ERealm.Hibernia;
			return ERealm.None;
		}

		public static ERealm[] GetItemTemplateRealm(DbItemTemplate item)
		{
			switch ((EObjectType)item.Object_Type)
			{
					//Albion
				case EObjectType.CrushingWeapon:
				case EObjectType.SlashingWeapon:
				case EObjectType.ThrustWeapon:
				case EObjectType.TwoHandedWeapon:
				case EObjectType.PolearmWeapon:
				case EObjectType.Staff:
				case EObjectType.Longbow:
				case EObjectType.Crossbow:
				case EObjectType.Flexible:
				case EObjectType.Plate:
				case EObjectType.Bolt:
					return new ERealm[] { ERealm.Albion };

					//Midgard
				case EObjectType.Sword:
				case EObjectType.Hammer:
				case EObjectType.Axe:
				case EObjectType.Spear:
				case EObjectType.CompositeBow:
				case EObjectType.Thrown:
				case EObjectType.LeftAxe:
				case EObjectType.HandToHand:
					return new ERealm[] { ERealm.Midgard };

					//Hibernia
				case EObjectType.Fired:
				case EObjectType.RecurvedBow:
				case EObjectType.Blades:
				case EObjectType.Blunt:
				case EObjectType.Piercing:
				case EObjectType.LargeWeapons:
				case EObjectType.CelticSpear:
				case EObjectType.Scythe:
				case EObjectType.Reinforced:
				case EObjectType.Scale:
					return new ERealm[] { ERealm.Hibernia };

					//Special
				case EObjectType.Studded:
				case EObjectType.Chain:
					return new ERealm[] { ERealm.Albion, ERealm.Midgard };

				case EObjectType.Instrument:
					return new ERealm[] { ERealm.Albion, ERealm.Hibernia };

					//Common Armor
				case EObjectType.Cloth:
				case EObjectType.Leather:
					//Misc
				case EObjectType.GenericItem:
				case EObjectType.GenericWeapon:
				case EObjectType.GenericArmor:
				case EObjectType.Magical:
				case EObjectType.Shield:
				case EObjectType.Arrow:
				case EObjectType.Poison:
				case EObjectType.AlchemyTincture:
				case EObjectType.SpellcraftGem:
				case EObjectType.GardenObject:
				case EObjectType.SiegeBalista:
				case EObjectType.SiegeCatapult:
				case EObjectType.SiegeCauldron:
				case EObjectType.SiegeRam:
				case EObjectType.SiegeTrebuchet:
					break;
			}

			ERealm realm = ERealm.None;

			if (item.Bonus1Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus1Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus2Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus2Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus3Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus3Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus4Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus4Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus5Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus5Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus6Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus6Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus7Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus7Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus8Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus8Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus9Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus9Type)) != ERealm.None)
				return new ERealm[] { realm };

			if (item.Bonus10Type > 0 && (realm = GetBonusRealm((EProperty)item.Bonus10Type)) != ERealm.None)
				return new ERealm[] { realm };

			return new ERealm[] { realm };

		}

		public static byte GetSpecToInternalIndex(string name)
		{
			switch (name)
			{
					case Specs.Slash: return 0x01;
					case Specs.Thrust: return 0x02;
					case Specs.Parry: return 0x08;
					case Specs.Sword: return 0x0E;
					case Specs.Hammer: return 0x10;
					case Specs.Axe: return 0x11;
					case Specs.Left_Axe: return 0x12;
					case Specs.Stealth: return 0x13;
					case Specs.Spear: return 0x1A;
					case Specs.Mending: return 0x1D;
					case Specs.Augmentation: return 0x1E;
					case Specs.Crush: return 0x21;
					case Specs.Pacification: return 0x22;
					//				case Specs.Cave_Magic:      return 0x25; ?
					case Specs.Darkness: return 0x26;
					case Specs.Suppression: return 0x27;
					case Specs.Runecarving: return 0x2A;
					case Specs.Shields: return 0x2B;
					case Specs.Flexible: return 0x2E;
					case Specs.Staff: return 0x2F;
					case Specs.Summoning: return 0x30;
					case Specs.Stormcalling: return 0x32;
					case Specs.Beastcraft: return 0x3E;
					case Specs.Polearms: return 0x40;
					case Specs.Two_Handed: return 0x41;
					case Specs.Fire_Magic: return 0x42;
					case Specs.Wind_Magic: return 0x43;
					case Specs.Cold_Magic: return 0x44;
					case Specs.Earth_Magic: return 0x45;
					case Specs.Light: return 0x46;
					case Specs.Matter_Magic: return 0x47;
					case Specs.Body_Magic: return 0x48;
					case Specs.Spirit_Magic: return 0x49;
					case Specs.Mind_Magic: return 0x4A;
					case Specs.Void: return 0x4B;
					case Specs.Mana: return 0x4C;
					case Specs.Dual_Wield: return 0x4D;
					case Specs.CompositeBow: return 0x4E;
					case Specs.Battlesongs: return 0x52;
					case Specs.Enhancement: return 0x53;
					case Specs.Enchantments: return 0x54;
					case Specs.Rejuvenation: return 0x58;
					case Specs.Smite: return 0x59;
					case Specs.Longbow: return 0x5A;
					case Specs.Crossbow: return 0x5B;
					case Specs.Chants: return 0x61;
					case Specs.Instruments: return 0x62;
					case Specs.Blades: return 0x65;
					case Specs.Blunt: return 0x66;
					case Specs.Piercing: return 0x67;
					case Specs.Large_Weapons: return 0x68;
					case Specs.Mentalism: return 0x69;
					case Specs.Regrowth: return 0x6A;
					case Specs.Nurture: return 0x6B;
					case Specs.Nature: return 0x6C;
					case Specs.Music: return 0x6D;
					case Specs.Celtic_Dual: return 0x6E;
					case Specs.Celtic_Spear: return 0x70;
					case Specs.RecurveBow: return 0x71;
					case Specs.Valor: return 0x72;
					case Specs.Pathfinding: return 0x74;
					case Specs.Envenom: return 0x75;
					case Specs.Critical_Strike: return 0x76;
					case Specs.Deathsight: return 0x78;
					case Specs.Painworking: return 0x79;
					case Specs.Death_Servant: return 0x7A;
					case Specs.Soulrending: return 0x7B;
					case Specs.HandToHand: return 0x7C;
					case Specs.Scythe: return 0x7D;
					//				case Specs.Bone_Army:       return 0x7E; ?
					case Specs.Arboreal_Path: return 0x7F;
					case Specs.Creeping_Path: return 0x81;
					case Specs.Verdant_Path: return 0x82;
					case Specs.OdinsWill: return 0x85;
					case Specs.SpectralForce: return 0x86; // Spectral Guard ?
					case Specs.PhantasmalWail: return 0x87;
					case Specs.EtherealShriek: return 0x88;
					case Specs.ShadowMastery: return 0x89;
					case Specs.VampiiricEmbrace: return 0x8A;
					case Specs.Dementia: return 0x8B;
					case Specs.Witchcraft: return 0x8C;
					case Specs.Cursing: return 0x8D;
					case Specs.Hexing: return 0x8E;
					case Specs.Fist_Wraps: return 0x93;
					case Specs.Mauler_Staff: return 0x94;
					case Specs.SpectralGuard: return 0x95;
					case Specs.Archery : return 0x9B;
					default: return 0;
			}
		}
		
		// webdisplay enums: they are processed via /webdisplay command
		public enum eWebDisplay: byte
		{
			all 		= 0x00,
			position 	= 0x01,
			template	= 0x02,
			equipment	= 0x04,
			craft		= 0x08,			
		}
		
		#region AllowedClassesRaces
		/// <summary>
		/// All possible player races
		/// </summary>
		public static readonly Dictionary<ERace, Dictionary<EStat, int>> STARTING_STATS_DICT = new Dictionary<ERace, Dictionary<EStat, int>>()
		{ 
			{ ERace.Unknown, new Dictionary<EStat, int>()			{{EStat.STR, 60}, {EStat.CON, 60}, {EStat.DEX, 60}, {EStat.QUI, 60}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Briton, new Dictionary<EStat, int>()			{{EStat.STR, 60}, {EStat.CON, 60}, {EStat.DEX, 60}, {EStat.QUI, 60}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Avalonian, new Dictionary<EStat, int>()			{{EStat.STR, 45}, {EStat.CON, 45}, {EStat.DEX, 60}, {EStat.QUI, 70}, {EStat.INT, 80}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Highlander, new Dictionary<EStat, int>()		{{EStat.STR, 70}, {EStat.CON, 70}, {EStat.DEX, 50}, {EStat.QUI, 50}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Saracen, new Dictionary<EStat, int>()			{{EStat.STR, 50}, {EStat.CON, 50}, {EStat.DEX, 80}, {EStat.QUI, 60}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Norseman, new Dictionary<EStat, int>()			{{EStat.STR, 70}, {EStat.CON, 70}, {EStat.DEX, 50}, {EStat.QUI, 50}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Troll, new Dictionary<EStat, int>()				{{EStat.STR, 100}, {EStat.CON, 70}, {EStat.DEX, 35}, {EStat.QUI, 35}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Dwarf, new Dictionary<EStat, int>()				{{EStat.STR, 60}, {EStat.CON, 80}, {EStat.DEX, 50}, {EStat.QUI, 50}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Kobold, new Dictionary<EStat, int>()			{{EStat.STR, 50}, {EStat.CON, 50}, {EStat.DEX, 70}, {EStat.QUI, 70}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Celt, new Dictionary<EStat, int>()				{{EStat.STR, 60}, {EStat.CON, 60}, {EStat.DEX, 60}, {EStat.QUI, 60}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Firbolg, new Dictionary<EStat, int>()			{{EStat.STR, 90}, {EStat.CON, 60}, {EStat.DEX, 40}, {EStat.QUI, 40}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 70}, {EStat.CHR, 60}, }},
			{ ERace.Elf, new Dictionary<EStat, int>()				{{EStat.STR, 40}, {EStat.CON, 40}, {EStat.DEX, 75}, {EStat.QUI, 75}, {EStat.INT, 70}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Lurikeen, new Dictionary<EStat, int>()			{{EStat.STR, 40}, {EStat.CON, 40}, {EStat.DEX, 80}, {EStat.QUI, 80}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Inconnu, new Dictionary<EStat, int>()			{{EStat.STR, 50}, {EStat.CON, 60}, {EStat.DEX, 70}, {EStat.QUI, 50}, {EStat.INT, 70}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Valkyn, new Dictionary<EStat, int>()			{{EStat.STR, 55}, {EStat.CON, 45}, {EStat.DEX, 65}, {EStat.QUI, 75}, {EStat.INT, 60}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			{ ERace.Sylvan, new Dictionary<EStat, int>()			{{EStat.STR, 70}, {EStat.CON, 60}, {EStat.DEX, 55}, {EStat.QUI, 45}, {EStat.INT, 70}, {EStat.PIE, 60}, {EStat.EMP, 60}, {EStat.CHR, 60}, }},
			// { eRace.HalfOgre, new Dictionary<eStat, int>()			{{eStat.STR, 90}, {eStat.CON, 70}, {eStat.DEX, 40}, {eStat.QUI, 40}, {eStat.INT, 60}, {eStat.PIE, 60}, {eStat.EMP, 60}, {eStat.CHR, 60}, }},
			// { eRace.Frostalf, new Dictionary<eStat, int>()			{{eStat.STR, 55}, {eStat.CON, 55}, {eStat.DEX, 55}, {eStat.QUI, 60}, {eStat.INT, 60}, {eStat.PIE, 75}, {eStat.EMP, 60}, {eStat.CHR, 60}, }},
			// { eRace.Shar, new Dictionary<eStat, int>()				{{eStat.STR, 60}, {eStat.CON, 80}, {eStat.DEX, 50}, {eStat.QUI, 50}, {eStat.INT, 60}, {eStat.PIE, 60}, {eStat.EMP, 60}, {eStat.CHR, 60}, }},
			// { eRace.AlbionMinotaur, new Dictionary<eStat, int>()	{{eStat.STR, 80}, {eStat.CON, 70}, {eStat.DEX, 50}, {eStat.QUI, 40}, {eStat.INT, 60}, {eStat.PIE, 60}, {eStat.EMP, 60}, {eStat.CHR, 60}, }},
			// { eRace.MidgardMinotaur, new Dictionary<eStat, int>()	{{eStat.STR, 80}, {eStat.CON, 70}, {eStat.DEX, 50}, {eStat.QUI, 40}, {eStat.INT, 60}, {eStat.PIE, 60}, {eStat.EMP, 60}, {eStat.CHR, 60}, }},
			// { eRace.HiberniaMinotaur, new Dictionary<eStat, int>()	{{eStat.STR, 80}, {eStat.CON, 70}, {eStat.DEX, 50}, {eStat.QUI, 40}, {eStat.INT, 60}, {eStat.PIE, 60}, {eStat.EMP, 60}, {eStat.CHR, 60}, }},
		};
		/// <summary>
		/// All possible player starting classes
		/// </summary>
		public static readonly Dictionary<ERealm, List<EPlayerClass>> STARTING_CLASSES_DICT = new Dictionary<ERealm, List<EPlayerClass>>()
		{
			// pre 1.93
			{ERealm.Albion, new List<EPlayerClass>() {EPlayerClass.Fighter, EPlayerClass.Acolyte, EPlayerClass.Mage, EPlayerClass.Elementalist, EPlayerClass.AlbionRogue, EPlayerClass.Disciple,
				// post 1.93
				EPlayerClass.Paladin, 		// Paladin = 1,
				EPlayerClass.Armsman, 		// Armsman = 2,
				EPlayerClass.Scout, 	    // Scout = 3,
				EPlayerClass.Minstrel, 	    // Minstrel = 4,
				EPlayerClass.Theurgist, 	// Theurgist = 5,
				EPlayerClass.Cleric, 		// Cleric = 6,
				EPlayerClass.Wizard, 	    // Wizard = 7,
				EPlayerClass.Sorcerer, 		// Sorcerer = 8,
				EPlayerClass.Infiltrator, 	// Infiltrator = 9,
				EPlayerClass.Friar, 		// Friar = 10,
				EPlayerClass.Mercenary, 	// Mercenary = 11,
				EPlayerClass.Necromancer, 	// Necromancer = 12,
				EPlayerClass.Cabalist, 		// Cabalist = 13,
				EPlayerClass.Fighter, 		// Fighter = 14,
				EPlayerClass.Elementalist, 	// Elementalist = 15,
				EPlayerClass.Acolyte, 		// Acolyte = 16,
				EPlayerClass.AlbionRogue, 	// AlbionRogue = 17,
				EPlayerClass.Mage, 			// Mage = 18,
				EPlayerClass.Reaver, 		// Reaver = 19,
				EPlayerClass.Disciple,		// Disciple = 20,
				// eCharacterClass.Heretic, 		// Heretic = 33,
				// eCharacterClass.MaulerAlb		// Mauler_Alb = 60,
			}},
			{ERealm.Midgard, new List<EPlayerClass>() {EPlayerClass.Viking, EPlayerClass.Mystic, EPlayerClass.Seer, EPlayerClass.MidgardRogue,
				// post 1.93
				EPlayerClass.Thane, 		// Thane = 21,
				EPlayerClass.Warrior, 		// Warrior = 22,
				EPlayerClass.Shadowblade, 	// Shadowblade = 23,
				EPlayerClass.Skald, 		// Skald = 24,
				EPlayerClass.Hunter, 	    // Hunter = 25,
				EPlayerClass.Healer, 		// Healer = 26,
				EPlayerClass.Spiritmaster,  // Spiritmaster = 27,
				EPlayerClass.Shaman, 		// Shaman = 28,
				EPlayerClass.Runemaster, 	// Runemaster = 29,
				EPlayerClass.Bonedancer, 	// Bonedancer = 30,
				EPlayerClass.Berserker, 	// Berserker = 31,
				EPlayerClass.Savage, 		// Savage = 32,
				EPlayerClass.Valkyrie, 		// Valkyrie = 34,
				EPlayerClass.Viking, 		// Viking = 35,
				EPlayerClass.Mystic, 		// Mystic = 36,
				EPlayerClass.Seer, 			// Seer = 37,
				EPlayerClass.MidgardRogue,	// MidgardRogue = 38,
				// eCharacterClass.Warlock, 		// Warlock = 59,
				// eCharacterClass.MaulerMid		// Mauler_Mid = 61,
			}},
			{ERealm.Hibernia, new List<EPlayerClass>() {EPlayerClass.Guardian, EPlayerClass.Stalker, EPlayerClass.Naturalist, EPlayerClass.Magician, EPlayerClass.Forester,
				// post 1.93
				EPlayerClass.Bainshee, 		// Bainshee = 39,
				EPlayerClass.Eldritch, 		// Eldritch = 40,
				EPlayerClass.Enchanter, 	// Enchanter = 41,
				EPlayerClass.Mentalist, 	// Mentalist = 42,
				EPlayerClass.Blademaster, 	// Blademaster = 43,
				EPlayerClass.Hero, 		    // Hero = 44,
				EPlayerClass.Champion, 		// Champion = 45,
				EPlayerClass.Warden, 	    // Warden = 46,
				EPlayerClass.Druid, 	    // Druid = 47,
				EPlayerClass.Bard, 	        // Bard = 48,
				EPlayerClass.Nightshade, 	// Nightshade = 49,
				EPlayerClass.Ranger, 		// Ranger = 50,
				EPlayerClass.Magician, 		// Magician = 51,
				EPlayerClass.Guardian, 		// Guardian = 52,
				EPlayerClass.Naturalist, 	// Naturalist = 53,
				EPlayerClass.Stalker, 		// Stalker = 54,
				EPlayerClass.Animist, 		// Animist = 55,
				EPlayerClass.Valewalker, 	// Valewalker = 56,
				EPlayerClass.Forester, 		// Forester = 57,
				// eCharacterClass.Vampiir, 		// Vampiir = 58,
				// eCharacterClass.MaulerHib	 	// Mauler_Hib = 62,
			}},
		};

		/// <summary>
		/// Race to Gender Constraints
		/// </summary>
		public static readonly Dictionary<ERace, EGender> RACE_GENDER_CONSTRAINTS_DICT = new Dictionary<ERace, EGender>()
		{
			{ERace.AlbionMinotaur, EGender.Male},
			{ERace.MidgardMinotaur, EGender.Male},
			{ERace.HiberniaMinotaur, EGender.Male},
		};
		
		/// <summary>
		/// Class to Gender Constraints
		/// </summary>
		public static readonly Dictionary<EPlayerClass, EGender> CLASS_GENDER_CONSTRAINTS_DICT = new Dictionary<EPlayerClass, EGender>()
		{
			{EPlayerClass.Valkyrie, EGender.Female},
			{EPlayerClass.Bainshee, EGender.Female},
		};
		
		/// <summary>
		/// Holds all realm rank names
		/// sirru mod 20.11.06
		/// </summary>
		public static string[, ,] REALM_RANK_NAMES = new string[,,]
		{
			// Albion
			{
				// Male
				{
					"Guardian",
					"Warder",
					"Myrmidon",
					"Gryphon Knight",
					"Eagle Knight",
					"Phoenix Knight",
					"Alerion Knight",
					"Unicorn Knight",
					"Lion Knight",
					"Dragon Knight",
					"Lord",
					"Baronet",
					"Baron",
					"Arch Duke"
				}
				,
				// Female
				{
					"Guardian",
					"Warder",
					"Myrmidon",
					"Gryphon Knight",
					"Eagle Knight",
					"Phoenix Knight",
					"Alerion Knight",
					"Unicorn Knight",
					"Lion Knight",
					"Dragon Knight",
					"Lady",
					"Baronetess",
					"Baroness",
					"Arch Duchess",
				}
			}
			,
			// Midgard
			{
				// Male
				{
					"Skiltvakten",
					"Isen Vakten",
					"Flammen Vakten",
					"Elding Vakten",
					"Stormur Vakten",
					"Isen Herra",
					"Flammen Herra",
					"Elding Herra",
					"Stormur Herra",
					"Einherjar",
					"Herra",
					"Hersir",
					"Vicomte",
					"Stor Jarl"
				}
				,
				// Female
				{
					"Skiltvakten",
					"Isen Vakten",
					"Flammen Vakten",
					"Elding Vakten",
					"Stormur Vakten",
					"Isen Fru",
					"Flammen Fru",
					"Elding Fru",
					"Stormur Fru",
					"Einherjar",
					"Fru",
					"Baronsfru",
					"Vicomtessa",
					"Stor Hurfru",
				}
			}
			,
			// Hibernia
			{
				// Male
				{
					"Savant",
					"Cosantoir",
					"Brehon",
					"Grove Protector",
					"Raven Ardent",
					"Silver Hand",
					"Thunderer",
					"Gilded Spear",
					"Tiarna",
					"Emerald Ridere",
					"Barun",
					"Ard Tiarna",
					"Ciann Cath",
					"Ard Diuc"
				}
				,
				// Female
				{
					"Savant",
					"Cosantoir",
					"Brehon",
					"Grove Protector",
					"Raven Ardent",
					"Silver Hand",
					"Thunderer",
					"Gilded Spear",
					"Bantiarna",
					"Emerald Ridere",
					"Banbharun",
					"Ard Bantiarna",
					"Ciann Cath",
					"Ard Bandiuc"
				}
			}
		};
		
		/// <summary>
		/// Translate Given Race/Gender Combo in Client Language
		/// </summary>
		/// <param name="client"></param>
		/// <param name="race"></param>
		/// <param name="gender"></param>
		/// <returns></returns>
		public static string RaceToTranslatedName(this GameClient client, int race, int gender)
		{
			ERace r = (ERace)race;
			string translationID = string.Format("GamePlayer.PlayerRace.{0}", r.ToString("F")); //Returns 'Unknown'

			if (r != 0)
			{
				switch ((EGender)gender)
				{
					case EGender.Female:
						translationID = string.Format("GamePlayer.PlayerRace.Female.{0}", r.ToString("F"));
						break;
					default:
						translationID = string.Format("GamePlayer.PlayerRace.Male.{0}", r.ToString("F"));
						break;
				}
			}
			
            return LanguageMgr.GetTranslation(client, translationID);
		}
		
		/// <summary>
		/// Translate Given Race/Gender Combo in Player Language
		/// </summary>
		/// <param name="player"></param>
		/// <param name="race"></param>
		/// <param name="gender"></param>
		/// <returns></returns>
		public static string RaceToTranslatedName(this GamePlayer player, int race, EGender gender)
		{
			if (player.Client != null)
				return player.Client.RaceToTranslatedName(race, (int)gender);
			
			return string.Format("!{0} - {1}!", ((ERace)race).ToString("F"), gender.ToString("F"));
		}
		#endregion
		
	}
	
	public static class Constants
	{
		public static int USE_AUTOVALUES = -1;
	}
}
