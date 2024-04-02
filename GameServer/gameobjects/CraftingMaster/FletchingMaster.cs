using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// the master for armorcrafting
	/// </summary>
	[NPCGuildScript("Fletchers Master")]
	public class FletchingMaster : CraftNPC
	{
		private static readonly eCraftingSkill[] m_trainedSkills = 
		{
			eCraftingSkill.ArmorCrafting,
			eCraftingSkill.ClothWorking,
			eCraftingSkill.Fletching,
			eCraftingSkill.LeatherCrafting,
			eCraftingSkill.SiegeCrafting,
			eCraftingSkill.Tailoring,
			eCraftingSkill.WeaponCrafting,
			eCraftingSkill.MetalWorking,
			eCraftingSkill.WoodWorking,
		};

		public override eCraftingSkill[] TrainedSkills
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

		public override eCraftingSkill TheCraftingSkill
		{
			get { return eCraftingSkill.Fletching; }
		}

		public override string InitialEntersentence
		{
			get 
			{ 
                return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FletchingMaster.InitialEntersentence");
            }
		}
	}
}
