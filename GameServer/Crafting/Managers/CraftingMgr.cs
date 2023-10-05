using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// Description r�sum�e de CraftingMgr.
	/// </summary>
	public class CraftingMgr
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Hold all crafting skill
		/// </summary>
		protected static ACraftingSkill[] m_craftingskills = new ACraftingSkill[(int)ECraftingSkill._Last];

		/// <summary>
		/// get a crafting skill by the enum index
		/// </summary>
		/// <param name="skill"></param>
		/// <returns></returns>
		public static ACraftingSkill getSkillbyEnum(ECraftingSkill skill)
		{
			if (skill == ECraftingSkill.NoCrafting) return null;
			return m_craftingskills[(int)skill - 1] as ACraftingSkill;
		}

		/// <summary>
		/// Initialize the crafting system
		/// </summary>
		/// <returns></returns>
		public static bool Init()
		{
			// skill
			m_craftingskills[(int)ECraftingSkill.ArmorCrafting - 1] = new Armorcrafting();
			m_craftingskills[(int)ECraftingSkill.Fletching - 1] = new Fletching();
			m_craftingskills[(int)ECraftingSkill.SiegeCrafting - 1] = new Siegecrafting();
			m_craftingskills[(int)ECraftingSkill.Tailoring - 1] = new Tailoring();
			m_craftingskills[(int)ECraftingSkill.WeaponCrafting - 1] = new Weaponcrafting();

			m_craftingskills[(int)ECraftingSkill.ClothWorking - 1] = new Clothworking();
			m_craftingskills[(int)ECraftingSkill.GemCutting - 1] = new Gemcutting();
			m_craftingskills[(int)ECraftingSkill.HerbalCrafting - 1] = new Herbcraft();
			m_craftingskills[(int)ECraftingSkill.LeatherCrafting - 1] = new Leathercrafting();
			m_craftingskills[(int)ECraftingSkill.MetalWorking - 1] = new Metalworking();
			m_craftingskills[(int)ECraftingSkill.WoodWorking - 1] = new Woodworking();
			m_craftingskills[(int)ECraftingSkill.BasicCrafting - 1] = new BasicCrafting();

			//Advanced skill
			m_craftingskills[(int)ECraftingSkill.Alchemy - 1] = new Alchemy();
			m_craftingskills[(int)ECraftingSkill.SpellCrafting - 1] = new Spellcrafting();

			return true;
		}

		#region Global craft functions

		/// <summary>
		/// Return the crafting skill which created the item
		/// </summary>
		public static ECraftingSkill GetCraftingSkill(DbInventoryItem item)
		{
			if (!item.IsCrafted)
				return ECraftingSkill.NoCrafting;

			switch (item.Object_Type)
			{
				case (int)eObjectType.Cloth:
				case (int)eObjectType.Leather:
					return ECraftingSkill.Tailoring;

				case (int)eObjectType.Studded:
				case (int)eObjectType.Reinforced:
				case (int)eObjectType.Chain:
				case (int)eObjectType.Scale:
				case (int)eObjectType.Plate:
					return ECraftingSkill.ArmorCrafting;

				// all weapon
				case (int)eObjectType.Axe:
				case (int)eObjectType.Blades:
				case (int)eObjectType.Blunt:
				case (int)eObjectType.CelticSpear:
				case (int)eObjectType.CrushingWeapon:
				case (int)eObjectType.Flexible:
				case (int)eObjectType.Hammer:
				case (int)eObjectType.HandToHand:
				case (int)eObjectType.LargeWeapons:
				case (int)eObjectType.LeftAxe:
				case (int)eObjectType.Piercing:
				case (int)eObjectType.PolearmWeapon:
				case (int)eObjectType.Scythe:
				case (int)eObjectType.Shield:
				case (int)eObjectType.SlashingWeapon:
				case (int)eObjectType.Spear:
				case (int)eObjectType.Sword:
				case (int)eObjectType.ThrustWeapon:
				case (int)eObjectType.TwoHandedWeapon:
					return ECraftingSkill.WeaponCrafting;

				case (int)eObjectType.CompositeBow:
				case (int)eObjectType.Crossbow:
				case (int)eObjectType.Fired:
				case (int)eObjectType.Instrument:
				case (int)eObjectType.Longbow:
				case (int)eObjectType.RecurvedBow:
				case (int)eObjectType.Staff:
					return ECraftingSkill.Fletching;

				case (int)eObjectType.AlchemyTincture:
				case (int)eObjectType.Poison:
					return ECraftingSkill.Alchemy;

				case (int)eObjectType.SpellcraftGem:
					return ECraftingSkill.SpellCrafting;

				case (int)eObjectType.SiegeBalista:
				case (int)eObjectType.SiegeCatapult:
				case (int)eObjectType.SiegeCauldron:
				case (int)eObjectType.SiegeRam:
				case (int)eObjectType.SiegeTrebuchet:
					return ECraftingSkill.SiegeCrafting;

				default:
					return ECraftingSkill.NoCrafting;
			}
		}

		/// <summary>
		/// Return the crafting skill needed to work on the item
		/// </summary>
		public static ECraftingSkill GetSecondaryCraftingSkillToWorkOnItem(DbInventoryItem item)
		{
			switch (item.Object_Type)
			{
				case (int)eObjectType.Cloth:
					return ECraftingSkill.ClothWorking;

				case (int)eObjectType.Leather:
				case (int)eObjectType.Studded:
					return ECraftingSkill.LeatherCrafting;

				// all weapon
				case (int)eObjectType.Axe:
				case (int)eObjectType.Blades:
				case (int)eObjectType.Blunt:
				case (int)eObjectType.CelticSpear:
				case (int)eObjectType.CrushingWeapon:
				case (int)eObjectType.Flexible:
				case (int)eObjectType.Hammer:
				case (int)eObjectType.HandToHand:
				case (int)eObjectType.LargeWeapons:
				case (int)eObjectType.LeftAxe:
				case (int)eObjectType.Piercing:
				case (int)eObjectType.PolearmWeapon:
				case (int)eObjectType.Scythe:
				case (int)eObjectType.Shield:
				case (int)eObjectType.SlashingWeapon:
				case (int)eObjectType.Spear:
				case (int)eObjectType.Sword:
				case (int)eObjectType.ThrustWeapon:
				case (int)eObjectType.TwoHandedWeapon:
				// all other armor
				case (int)eObjectType.Chain:
				case (int)eObjectType.Plate:
				case (int)eObjectType.Reinforced:
				case (int)eObjectType.Scale:
					return ECraftingSkill.MetalWorking;

				case (int)eObjectType.CompositeBow:
				case (int)eObjectType.Crossbow:
				case (int)eObjectType.Fired:
				case (int)eObjectType.Instrument:
				case (int)eObjectType.Longbow:
				case (int)eObjectType.RecurvedBow:
				case (int)eObjectType.Staff:
					return ECraftingSkill.WoodWorking;
				
				case (int)eObjectType.Magical:
					return ECraftingSkill.GemCutting;

				default:
					return ECraftingSkill.NoCrafting;
			}
		}

		/// <summary>
		/// Return the approximative craft level of the item
		/// </summary>
		public static int GetItemCraftLevel(DbInventoryItem item)
		{
			switch (item.Object_Type)
			{
				case (int)eObjectType.Cloth:
				case (int)eObjectType.Leather:
				case (int)eObjectType.Studded:
				case (int)eObjectType.Chain:
				case (int)eObjectType.Plate:
				case (int)eObjectType.Reinforced:
				case (int)eObjectType.Scale:
					{
						int baseLevel = 15 + item.Level * 20; // gloves
						switch (item.Item_Type)
						{
							case (int)eInventorySlot.HeadArmor: // head
								return baseLevel + 15;

							case (int)eInventorySlot.FeetArmor: // feet
								return baseLevel + 30;

							case (int)eInventorySlot.LegsArmor: // legs
								return baseLevel + 50;

							case (int)eInventorySlot.ArmsArmor: // arms
								return baseLevel + 65;

							case (int)eInventorySlot.TorsoArmor: // torso
								return baseLevel + 80;

							default:
								return baseLevel;
						}
					}

				case (int)eObjectType.Axe:
				case (int)eObjectType.Blades:
				case (int)eObjectType.Blunt:
				case (int)eObjectType.CelticSpear:
				case (int)eObjectType.CrushingWeapon:
				case (int)eObjectType.Flexible:
				case (int)eObjectType.Hammer:
				case (int)eObjectType.HandToHand:
				case (int)eObjectType.LargeWeapons:
				case (int)eObjectType.LeftAxe:
				case (int)eObjectType.Piercing:
				case (int)eObjectType.PolearmWeapon:
				case (int)eObjectType.Scythe:
				case (int)eObjectType.Shield:
				case (int)eObjectType.SlashingWeapon:
				case (int)eObjectType.Spear:
				case (int)eObjectType.Sword:
				case (int)eObjectType.ThrustWeapon:
				case (int)eObjectType.TwoHandedWeapon:

				case (int)eObjectType.CompositeBow:
				case (int)eObjectType.Crossbow:
				case (int)eObjectType.Fired:
				case (int)eObjectType.Instrument:
				case (int)eObjectType.Longbow:
				case (int)eObjectType.RecurvedBow:
				case (int)eObjectType.Staff:
				case (int)eObjectType.Magical:
					return 15 + (item.Level - 1) * 20;

				default:
					return 0;
			}
		}

		#endregion

	}
}
