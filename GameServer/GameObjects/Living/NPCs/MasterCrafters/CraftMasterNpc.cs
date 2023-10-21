using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS;

public abstract class CraftMasterNpc : GameNpc
{
	public abstract string GUILD_ORDER { get; }

	public abstract string ACCEPTED_BY_ORDER_NAME { get; }

	public abstract ECraftingSkill[] TrainedSkills { get; }

	public abstract ECraftingSkill TheCraftingSkill { get; }

	public abstract string InitialEntersentence { get; }

	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;
		if (player.PlayerClass == null)
			return false;

		TurnTo(player, 5000);

		var playerSkills = player.GetCraftingSkillValue(TheCraftingSkill);
		if (playerSkills >= 1)
		{
			player.CraftingPrimarySkill = TheCraftingSkill;
			SayTo(player, EChatLoc.CL_PopupWindow, "Hello, " + player.CraftTitle.GetDescription(player) + "! Because you are already a member of our order, you do not need to join. I have changed your primary crafting skill to " + player.CraftingPrimarySkill + ". Please speak to myself or another craft master if you wish to change your primary crafting skill again.");
			player.Out.SendUpdatePlayer();
			player.Out.SendUpdateCraftingSkills();
			return true;
		}
		
		// Dunnerholl : Basic Crafting Master does not give the option to rejoin this craft
		if (InitialEntersentence != null)
		{
			SayTo(player, EChatLoc.CL_PopupWindow, InitialEntersentence);
		}
        
            	
		return true;
	}

	public override bool WhisperReceive(GameLiving source, string text)
	{
		if (!base.WhisperReceive(source, text))
			return false;
		if (source is GamePlayer == false)
			return true;

		GamePlayer player = (GamePlayer) source;
		var playerSkills = player.GetCraftingSkillValue(TheCraftingSkill);
		
		if (playerSkills >= 1 && text == GUILD_ORDER)
			return false;
        if(text == GUILD_ORDER)
		{
			player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.WhisperReceive.WishToJoin", ACCEPTED_BY_ORDER_NAME), new CustomDialogResponse(CraftNpcDialogResponse));
		}
		return true;
	}

	protected virtual void CraftNpcDialogResponse(GamePlayer player, byte response)
	{
		if (response != 0x01)
			return; //declined

		player.CraftingPrimarySkill = TheCraftingSkill;

		player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.CraftNpcDialogResponse.Accepted", ACCEPTED_BY_ORDER_NAME), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			
		foreach (ECraftingSkill skill in TrainedSkills)
		{
			player.AddCraftingSkill(skill, 1);
		}
		player.Out.SendUpdatePlayer();
		player.Out.SendUpdateCraftingSkills();
		
	}
}