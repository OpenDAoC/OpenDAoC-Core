using Core.GS.Enums;
using Core.Language;

namespace Core.GS;

[NpcGuildScript("Tailors Master")]
public class TailoringMaster : CraftMasterNpc
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
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "TailorsMaster.GuildOrder");
        }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "TailorsMaster.AcceptedByOrderName");
        }
	}

	public override ECraftingSkill TheCraftingSkill
	{
		get { return ECraftingSkill.Tailoring; }
	}

	public override string InitialEntersentence
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "TailorsMaster.InitialEntersentence");
        }
	}
}