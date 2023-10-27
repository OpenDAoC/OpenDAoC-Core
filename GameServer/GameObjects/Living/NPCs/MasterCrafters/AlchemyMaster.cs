using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS;

[NpcGuildScript("Alchemists Master")]
public class AlchemyMaster : CraftMasterNpc
{
	private static readonly ECraftingSkill[] m_trainedSkills = 
	{
		ECraftingSkill.SpellCrafting,
		ECraftingSkill.Alchemy,
		ECraftingSkill.GemCutting,
		ECraftingSkill.HerbalCrafting,
		ECraftingSkill.SiegeCrafting,
	};

	public override ECraftingSkill[] TrainedSkills
	{
		get { return m_trainedSkills; }
	}

	public override string GUILD_ORDER
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "AlchemistsMaster.GuildOrder");
        }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "AlchemistsMaster.AcceptedByOrderName");
        }
	}
	public override ECraftingSkill TheCraftingSkill
	{
		get { return ECraftingSkill.Alchemy; }
	}
	public override string InitialEntersentence
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "AlchemistsMaster.InitialEntersentence");
        }
	}
}