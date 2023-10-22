using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS;

// This class has to be completed and may be inherited for scripting purpose
public class ChampionMasterNpc : GameNpc
{
    public ChampionMasterNpc()
        : base()
    {
    }
    /// <summary>
    /// Talk to trainer
    /// </summary>
    /// <param name="source"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public override bool WhisperReceive(GameLiving source, string text)
    {
        if (!base.WhisperReceive(source, text)) return false;
        GamePlayer player = source as GamePlayer;
        if (player == null) return false;

        switch (text)
        {
            //level respec for players
            case "respecialize":
                if (player.Champion && player.ChampionLevel >= 5)
                {
                    player.RespecChampionSkills();
                    player.Out.SendMessage("I have reset your Champion skills!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);

                }
                break;
        }

        //Now we turn the npc into the direction of the person
        TurnTo(player, 10000);
        return true;
    }

    /// <summary>
    /// For Recieving CL Respec Stone.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
    {
        if (source == null || item == null) return false;

        GamePlayer player = source as GamePlayer;
        if (player != null)
        {
            switch (item.Id_nb)
            {
                case "respec_cl":
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Other, item.Template);
                    player.RespecAmountChampionSkill++;
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "CLWeaponNPC.ReceiveItem.RespecCL"), EChatType.CT_System, EChatLoc.CL_PopupWindow);
                    return true;
                }
            }
        }

        return base.ReceiveItem(source, item);
    }
}