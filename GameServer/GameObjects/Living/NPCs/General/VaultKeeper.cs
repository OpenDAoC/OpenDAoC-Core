using System.Collections;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS;

[NpcGuildScript("Vault Keeper")]
public class GameVaultKeeper : GameNpc
{
	/// <summary>
	/// Constructor
	/// </summary>
	public GameVaultKeeper() : base()
	{
	}

	#region Examine Message

	/// <summary>
	/// Adds messages to ArrayList which are sent when object is targeted
	/// </summary>
	/// <param name="player">GamePlayer that is examining this object</param>
	/// <returns>list with string messages</returns>
	public override IList GetExamineMessages(GamePlayer player)
	{
		IList list = new ArrayList();
        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "VaultKeeper.YouExamine", GetName(0, false, player.Client.Account.Language, this),
                                                   GetPronoun(0, true, player.Client.Account.Language), GetAggroLevelString(player, false)));
        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "VaultKeeper.RightClick"));
        return list;
	}

	#endregion Examine Message

	#region Interact

	/// <summary>
	/// Called when a player right clicks on the vaultkeeper
	/// </summary>
	/// <param name="player">Player that interacted with the vaultkeeper</param>
	/// <returns>True if succeeded</returns>
	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;

		if (player.ActiveInventoryObject != null)
		{
			player.ActiveInventoryObject.RemoveObserver(player);
		}

		player.ActiveInventoryObject = null;

		TurnTo(player, 10000);
		var items = player.Inventory.GetItemRange(EInventorySlot.FirstVault, EInventorySlot.LastVault);
		player.Out.SendInventoryItemsUpdate(EInventoryWindowType.PlayerVault, items.Count > 0 ? items : null);
		return true;
	}

	#endregion examine message
}