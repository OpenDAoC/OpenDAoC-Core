using DOL.Language;

namespace DOL.GS
{
	[NPCGuildScript("Basic Crafters Master")]
	public class BasicCraftingMaster: CraftNPC
	{
		private static readonly eCraftingSkill[] m_trainedSkills = {
			eCraftingSkill.Alchemy,
			eCraftingSkill.ArmorCrafting,
			eCraftingSkill.BasicCrafting,
			eCraftingSkill.ClothWorking,
			eCraftingSkill.Fletching,
			eCraftingSkill.GemCutting,
			eCraftingSkill.HerbalCrafting,
			eCraftingSkill.LeatherCrafting,
			eCraftingSkill.MetalWorking,
			eCraftingSkill.SiegeCrafting,
			eCraftingSkill.SpellCrafting,
			eCraftingSkill.Tailoring,
			eCraftingSkill.WeaponCrafting,
			eCraftingSkill.WoodWorking
		};

		public override eCraftingSkill[] TrainedSkills
		{
			get { return m_trainedSkills; }
		}

		public override eCraftingSkill TheCraftingSkill
		{
			get { return eCraftingSkill.BasicCrafting; }
		}

		public override string GUILD_ORDER
		{
            get { return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "BasicCraftingMaster.GuildOrder"); }
		}

		public override string ACCEPTED_BY_ORDER_NAME
		{
            get { return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "BasicCraftingMaster.AcceptedByOrderName"); }
		}
		public override string InitialEntersentence
		{
			// Dunnerholl : Basic Crafting Master does not give the option to rejoin this craft
			get 
			{ 
				return null; 
			}
		}
	}
}
