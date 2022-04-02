using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS;

public class BountyManager

{
    public const string KILLEDBY = "KilledBy";

    public static List<BountyPoster> ActiveBounties;

    [ScriptLoadedEvent]
    public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
    {
        GameEventMgr.AddHandler(GamePlayerEvent.Dying, new DOLEventHandler(GreyPlayerKilled));
    }
    
    public BountyManager()
    {
    }

    private void ResetBounty()
    {
        if (ActiveBounties == null)
            ActiveBounties = new List<BountyPoster>();
        ActiveBounties.Clear();
    }

    public static List<BountyPoster> GetActiveBounties
    {
        get
        {
            return ActiveBounties;
        }
        set { }
    }
    
    private static void GreyPlayerKilled(DOLEvent e, object sender, EventArgs args)
    {
        GamePlayer player = sender as GamePlayer;

        if (player == null) return;
        
        if (e != GameLivingEvent.Dying) return;
        
        DyingEventArgs eArgs = args as DyingEventArgs;

        if (eArgs.Killer is not GamePlayer) return;
        
        GamePlayer killer = eArgs.Killer as GamePlayer;
        
        if (player.Realm == killer?.Realm) return;
        
        if (!(killer?.GetConLevel(player) <= -3)) return;
        
        player.TempProperties.setProperty(KILLEDBY, killer);
        player.Out.SendMessage($"Use /bounty add <amount> if you want to call a bounty for {killer.Name}'s head!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
    }
    
    public static void AddBounty(GamePlayer killed, GamePlayer killer,  int amount = 50)
    {
        if (amount < 50) amount = 50;
        killed.Out.SendMessage($"You have called the head of {killer.Name} for {amount} gold!", eChatType.CT_System,
            eChatLoc.CL_SystemWindow);
        // killed.TempProperties.removeProperty(KILLEDBY);
            
        BountyPoster poster = new BountyPoster(killed, killer, killed.CurrentZone, amount);
        
        if(ActiveBounties == null)
        {
            ActiveBounties = new List<BountyPoster>();
        }
        
        
        if(ActiveBounties.Count() > 0)
        {
            //search for existing killer and increment if they exist, add them to the list if they don't
            var activePoster = GetActiveBountyForPlayer(killer);
            if (activePoster != null)
            {
                activePoster.Reward += amount;
                activePoster.LastSeenZone = killed.CurrentZone;
            }
            else
            {
                ActiveBounties.Add(poster);
            }
        }
        else
        {
            //add if its the first entry
            ActiveBounties.Add(poster);
        }
        
        
        
        BroadcastBounty(poster);

    }

    public static BountyPoster GetActiveBountyForPlayer(GamePlayer player)
    {
        return ActiveBounties.FirstOrDefault(x => x.Target.Name.Equals(player.Name));
    }

    private static void BroadcastBounty(BountyPoster poster)
    {
        foreach (var client in WorldMgr.GetAllPlayingClients())
        {
            if (client.Player.Realm != poster.Ganked.Realm) continue;

            var message =
                $"{poster.Ganked.Name} is offering {poster.Reward} gold for the head of {poster.Target.Name} in {poster.Target.CurrentZone.Description}";
                
            client.Player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller,
                eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(message, eChatType.CT_Broadcast,
                eChatLoc.CL_SystemWindow);
        }
    }
    
    public static IList<string> GetTextList()
    {
        List<string> temp = new List<string>();
        temp.Clear();

        if (ActiveBounties == null || ActiveBounties.Count == 0)
        {
            temp.Add("No active bounties.");
            return temp;
        }

        var count = 0;
        foreach (BountyPoster bounty in ActiveBounties)
        {
            count++;
            Console.WriteLine($"{count}. {bounty.Target.Name} - {bounty.Reward}");
            temp.Add($"{bounty.Ganked.Name} is offering {bounty.Reward} gold on {bounty.Target.Name}'s head!");
        }
        
        return temp;
    }
}