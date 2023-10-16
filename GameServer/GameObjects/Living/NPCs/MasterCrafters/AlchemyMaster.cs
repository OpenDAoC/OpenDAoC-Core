using DOL.Language;

namespace DOL.GS;

[NpcGuildScript("Alchemists Master")]
public class AlchemistsMaster : CraftMasterNpc
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
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AlchemistsMaster.GuildOrder");
        }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AlchemistsMaster.AcceptedByOrderName");
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
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AlchemistsMaster.InitialEntersentence");
        }
	}
}