using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS;

[NpcGuildScript("Spellcrafters Master")]
public class SpellCraftingMaster : CraftMasterNpc
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
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SpellCraftingMaster.GuildOrder");
        }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SpellCraftingMaster.AcceptedByOrderName");
        }
	}

	public override ECraftingSkill TheCraftingSkill
	{
		get { return ECraftingSkill.SpellCrafting; }
	}

	public override string InitialEntersentence
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SpellCraftingMaster.InitialEntersentence");
        }
	}
}