using System.Collections;
using Core.Database;
using Core.Database.Tables;
using Core.Language;

namespace Core.GS;

public class GameHousingHastener : GameMerchant
{
	public override ENpcFlags Flags
	{
		get { return ENpcFlags.PEACE; }
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public GameHousingHastener()
		: base()
	{

	}

	public const int SPEEDOFTHEREALMID = 2430;

	#region Examine/Interact Message

	/// <summary>
	/// Adds messages to ArrayList which are sent when object is targeted
	/// </summary>
	/// <param name="player">GamePlayer that is examining this object</param>
	/// <returns>list with string messages</returns>
	public override IList GetExamineMessages(GamePlayer player)
	{
		IList list = new ArrayList();
		list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language,
			"GameHousingHastener.GetExamineMessages.Examine", GetName(0, false), GetPronoun(0, true),
			GetAggroLevelString(player, false)));
		return list;
	}

	public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
	{
		if (source == null || item == null)
			return false;

		GamePlayer player = source as GamePlayer;
		if (player == null)
			return false;

		if (item.Id_nb == "Music_Ticket")
		{
			TargetObject = player;
			CastSpell(SkillBase.GetSpellByID(GameHastener.SPEEDOFTHEREALMID),
				SkillBase.GetSpellLine(GlobalSpellsLines.Realm_Spells));
			player.Inventory.RemoveItem(item);
			InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template,
				item.Count);
		}

		return true;
	}

	#endregion Examine/Interact Message
}