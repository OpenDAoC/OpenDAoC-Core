using Core.Language;

namespace Core.GS;

[NpcGuildScript("Siegecrafting Master")]
public class SiegecraftingMaster : CraftMasterNpc
{
	private static readonly ECraftingSkill[] m_trainedSkills = 
	{
		ECraftingSkill.MetalWorking,
		ECraftingSkill.WoodWorking,
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
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SiegecraftingMaster.GuildOrder");
        }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SiegecraftingMaster.AcceptedByOrderName");
        }
	}

	/// <summary>
	/// The eCraftingSkill
	/// </summary>
	public override ECraftingSkill TheCraftingSkill
	{
		get { return ECraftingSkill.SiegeCrafting; }
	}

	/// <summary>
	/// The text for join order
	/// </summary>
	public override string InitialEntersentence
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SiegecraftingMaster.InitialEntersentence");
        }
	}
}