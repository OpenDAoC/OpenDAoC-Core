using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS;

[NpcGuildScript("Weaponsmiths Master")]
public class WeaponCraftingMaster : CraftMasterNpc
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
            return LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "WeaponCraftingMaster.GuildOrder");
        }
	}

	public override string ACCEPTED_BY_ORDER_NAME
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "WeaponCraftingMaster.AcceptedByOrderName");
        }
	}

	public override ECraftingSkill TheCraftingSkill
	{
		get { return ECraftingSkill.WeaponCrafting; }
	}

	public override string InitialEntersentence
	{
		get
		{
            return LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "WeaponCraftingMaster.InitialEntersentence");
        }
	}
}