using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// the master for spell crafting
	/// </summary>
	[NPCGuildScript("Spellcrafters Master")]
	public class SpellCraftingMaster : CraftNPC
	{
		private static readonly eCraftingSkill[] m_trainedSkills = 
		{
			eCraftingSkill.SpellCrafting,
			eCraftingSkill.Alchemy,
			eCraftingSkill.GemCutting,
			eCraftingSkill.HerbalCrafting,
			eCraftingSkill.SiegeCrafting,
		};

		public override eCraftingSkill[] TrainedSkills
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

		public override eCraftingSkill TheCraftingSkill
		{
			get { return eCraftingSkill.SpellCrafting; }
		}

		public override string InitialEntersentence
		{
			get
			{
                return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SpellCraftingMaster.InitialEntersentence");
            }
		}
	}
}
