using DOL.Language;

namespace DOL.GS
{
	[NPCGuildScript("Siegecrafting Master")]
	public class SiegecraftingMaster : CraftNPC
	{
		private static readonly eCraftingSkill[] m_trainedSkills = 
		{
			eCraftingSkill.MetalWorking,
			eCraftingSkill.WoodWorking,
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
		public override eCraftingSkill TheCraftingSkill
		{
			get { return eCraftingSkill.SiegeCrafting; }
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
}
