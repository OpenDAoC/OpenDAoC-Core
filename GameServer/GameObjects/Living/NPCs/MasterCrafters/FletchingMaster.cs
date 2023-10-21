using Core.Language;

namespace Core.GS;

[NpcGuildScript("Fletchers Master")]
public class FletchingMaster : CraftMasterNpc
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
		ECraftingSkill.WoodWorking,
	};

	public override ECraftingSkill[] TrainedSkills
	{
		get { return m_trainedSkills; }
	}

	public override string GUILD_ORDER
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FletchingMaster.GuildOrder");
        }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FletchingMaster.AcceptedByOrderName");
        }
	}

	public override ECraftingSkill TheCraftingSkill
	{
		get { return ECraftingSkill.Fletching; }
	}

	public override string InitialEntersentence
	{
		get 
		{ 
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FletchingMaster.InitialEntersentence");
        }
	}
}