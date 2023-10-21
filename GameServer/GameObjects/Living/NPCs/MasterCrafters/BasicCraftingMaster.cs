using Core.GS.Enums;
using Core.Language;

namespace Core.GS;

[NpcGuildScript("Basic Crafters Master")]
public class BasicCraftingMaster: CraftMasterNpc
{
	private static readonly ECraftingSkill[] m_trainedSkills =
	{
		ECraftingSkill.Alchemy,
		ECraftingSkill.ArmorCrafting,
		ECraftingSkill.BasicCrafting,
		ECraftingSkill.ClothWorking,
		ECraftingSkill.Fletching,
		ECraftingSkill.GemCutting,
		ECraftingSkill.HerbalCrafting,
		ECraftingSkill.LeatherCrafting,
		ECraftingSkill.MetalWorking,
		ECraftingSkill.SiegeCrafting,
		ECraftingSkill.SpellCrafting,
		ECraftingSkill.Tailoring,
		ECraftingSkill.WeaponCrafting,
		ECraftingSkill.WoodWorking
	};

	public override ECraftingSkill[] TrainedSkills
	{
		get { return m_trainedSkills; }
	}

	public override ECraftingSkill TheCraftingSkill
	{
		get { return ECraftingSkill.BasicCrafting; }
	}

	public override string GUILD_ORDER
	{
        get { return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "BasicCraftingMaster.GuildOrder"); }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
        get { return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "BasicCraftingMaster.AcceptedByOrderName"); }
	}
	public override string InitialEntersentence
	{
		// Dunnerholl : Basic Crafting Master does not give the option to rejoin this craft
		get 
		{ 
			return null; 
		}
	}
}