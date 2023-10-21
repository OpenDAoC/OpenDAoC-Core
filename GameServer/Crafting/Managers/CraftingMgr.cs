using System.Reflection;
using Core.Database.Tables;
using log4net;

namespace Core.GS.Crafting;

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
			case (int)EObjectType.Cloth:
			case (int)EObjectType.Leather:
				return ECraftingSkill.Tailoring;

			case (int)EObjectType.Studded:
			case (int)EObjectType.Reinforced:
			case (int)EObjectType.Chain:
			case (int)EObjectType.Scale:
			case (int)EObjectType.Plate:
				return ECraftingSkill.ArmorCrafting;

			// all weapon
			case (int)EObjectType.Axe:
			case (int)EObjectType.Blades:
			case (int)EObjectType.Blunt:
			case (int)EObjectType.CelticSpear:
			case (int)EObjectType.CrushingWeapon:
			case (int)EObjectType.Flexible:
			case (int)EObjectType.Hammer:
			case (int)EObjectType.HandToHand:
			case (int)EObjectType.LargeWeapons:
			case (int)EObjectType.LeftAxe:
			case (int)EObjectType.Piercing:
			case (int)EObjectType.PolearmWeapon:
			case (int)EObjectType.Scythe:
			case (int)EObjectType.Shield:
			case (int)EObjectType.SlashingWeapon:
			case (int)EObjectType.Spear:
			case (int)EObjectType.Sword:
			case (int)EObjectType.ThrustWeapon:
			case (int)EObjectType.TwoHandedWeapon:
				return ECraftingSkill.WeaponCrafting;

			case (int)EObjectType.CompositeBow:
			case (int)EObjectType.Crossbow:
			case (int)EObjectType.Fired:
			case (int)EObjectType.Instrument:
			case (int)EObjectType.Longbow:
			case (int)EObjectType.RecurvedBow:
			case (int)EObjectType.Staff:
				return ECraftingSkill.Fletching;

			case (int)EObjectType.AlchemyTincture:
			case (int)EObjectType.Poison:
				return ECraftingSkill.Alchemy;

			case (int)EObjectType.SpellcraftGem:
				return ECraftingSkill.SpellCrafting;

			case (int)EObjectType.SiegeBalista:
			case (int)EObjectType.SiegeCatapult:
			case (int)EObjectType.SiegeCauldron:
			case (int)EObjectType.SiegeRam:
			case (int)EObjectType.SiegeTrebuchet:
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
			case (int)EObjectType.Cloth:
				return ECraftingSkill.ClothWorking;

			case (int)EObjectType.Leather:
			case (int)EObjectType.Studded:
				return ECraftingSkill.LeatherCrafting;

			// all weapon
			case (int)EObjectType.Axe:
			case (int)EObjectType.Blades:
			case (int)EObjectType.Blunt:
			case (int)EObjectType.CelticSpear:
			case (int)EObjectType.CrushingWeapon:
			case (int)EObjectType.Flexible:
			case (int)EObjectType.Hammer:
			case (int)EObjectType.HandToHand:
			case (int)EObjectType.LargeWeapons:
			case (int)EObjectType.LeftAxe:
			case (int)EObjectType.Piercing:
			case (int)EObjectType.PolearmWeapon:
			case (int)EObjectType.Scythe:
			case (int)EObjectType.Shield:
			case (int)EObjectType.SlashingWeapon:
			case (int)EObjectType.Spear:
			case (int)EObjectType.Sword:
			case (int)EObjectType.ThrustWeapon:
			case (int)EObjectType.TwoHandedWeapon:
			// all other armor
			case (int)EObjectType.Chain:
			case (int)EObjectType.Plate:
			case (int)EObjectType.Reinforced:
			case (int)EObjectType.Scale:
				return ECraftingSkill.MetalWorking;

			case (int)EObjectType.CompositeBow:
			case (int)EObjectType.Crossbow:
			case (int)EObjectType.Fired:
			case (int)EObjectType.Instrument:
			case (int)EObjectType.Longbow:
			case (int)EObjectType.RecurvedBow:
			case (int)EObjectType.Staff:
				return ECraftingSkill.WoodWorking;
			
			case (int)EObjectType.Magical:
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
			case (int)EObjectType.Cloth:
			case (int)EObjectType.Leather:
			case (int)EObjectType.Studded:
			case (int)EObjectType.Chain:
			case (int)EObjectType.Plate:
			case (int)EObjectType.Reinforced:
			case (int)EObjectType.Scale:
				{
					int baseLevel = 15 + item.Level * 20; // gloves
					switch (item.Item_Type)
					{
						case (int)EInventorySlot.HeadArmor: // head
							return baseLevel + 15;

						case (int)EInventorySlot.FeetArmor: // feet
							return baseLevel + 30;

						case (int)EInventorySlot.LegsArmor: // legs
							return baseLevel + 50;

						case (int)EInventorySlot.ArmsArmor: // arms
							return baseLevel + 65;

						case (int)EInventorySlot.TorsoArmor: // torso
							return baseLevel + 80;

						default:
							return baseLevel;
					}
				}

			case (int)EObjectType.Axe:
			case (int)EObjectType.Blades:
			case (int)EObjectType.Blunt:
			case (int)EObjectType.CelticSpear:
			case (int)EObjectType.CrushingWeapon:
			case (int)EObjectType.Flexible:
			case (int)EObjectType.Hammer:
			case (int)EObjectType.HandToHand:
			case (int)EObjectType.LargeWeapons:
			case (int)EObjectType.LeftAxe:
			case (int)EObjectType.Piercing:
			case (int)EObjectType.PolearmWeapon:
			case (int)EObjectType.Scythe:
			case (int)EObjectType.Shield:
			case (int)EObjectType.SlashingWeapon:
			case (int)EObjectType.Spear:
			case (int)EObjectType.Sword:
			case (int)EObjectType.ThrustWeapon:
			case (int)EObjectType.TwoHandedWeapon:

			case (int)EObjectType.CompositeBow:
			case (int)EObjectType.Crossbow:
			case (int)EObjectType.Fired:
			case (int)EObjectType.Instrument:
			case (int)EObjectType.Longbow:
			case (int)EObjectType.RecurvedBow:
			case (int)EObjectType.Staff:
			case (int)EObjectType.Magical:
				return 15 + (item.Level - 1) * 20;

			default:
				return 0;
		}
	}

	#endregion

}