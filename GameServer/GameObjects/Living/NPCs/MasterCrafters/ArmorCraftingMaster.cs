using Core.Language;

namespace Core.GS;

[NpcGuildScript("Armorsmiths Master")]
public class ArmorCraftingMaster : CraftMasterNpc
{
	private static readonly ECraftingSkill[] m_trainedSkills = 
	{
		ECraftingSkill.ArmorCrafting,
		ECraftingSkill.ClothWorking,
		ECraftingSkill.Fletching,
		ECraftingSkill.LeatherCrafting,
		ECraftingSkill.SiegeCrafting,
		ECraftingSkill.Tailoring,
		ECraftingSkill.WeaponCrafting,
		ECraftingSkill.MetalWorking,
		ECraftingSkill.WoodWorking
	};

	public override ECraftingSkill[] TrainedSkills
	{
		get { return m_trainedSkills; }
	}

	public override string GUILD_ORDER
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ArmorCraftingMaster.GuildOrder");
        }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ArmorCraftingMaster.AcceptedByOrderName");
        }
	}

	public override ECraftingSkill TheCraftingSkill
	{
		get { return ECraftingSkill.ArmorCrafting; }
	}

	public override string InitialEntersentence
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ArmorCraftingMaster.InitialEntersentence");
        }
	}
}