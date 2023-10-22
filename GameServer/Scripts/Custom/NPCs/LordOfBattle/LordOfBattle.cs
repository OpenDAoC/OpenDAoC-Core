using System.Collections.Generic;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameLoop;
using Core.GS.GameUtils;

namespace Core.GS.Scripts.Custom;

public class LordOfBattle : GameTrainingDummy {
   

    public override bool AddToWorld()
    {
        Name = "Mordred";
        GuildName = "Lord Of Battle";
        Realm = 0;
        Model = 1903;
        Size = 250;
        Level = 75;
        Inventory = new GameNpcInventory(GameNpcInventoryTemplate.EmptyTemplate);
        SetOwnBrain(new LordOfBattleBrain());

        return base.AddToWorld(); // Finish up and add him to the world.
    }

	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player)) return false;
		TurnTo(player.X, player.Y);

		
			player.Out.SendMessage("Greetings, " + player.PlayerClass.Name + ".\n\n" + "If you desire, I can port you back to your realm's [event zone]", EChatType.CT_Say, EChatLoc.CL_PopupWindow);

        if (player.effectListComponent.ContainsEffectForEffectType(EEffect.ResurrectionIllness))
        {
            EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, EEffect.ResurrectionIllness));
        }

        if (player.effectListComponent.ContainsEffectForEffectType(EEffect.RvrResurrectionIllness))
        {
            EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, EEffect.RvrResurrectionIllness));
        }


        if (player.InCombatPvPInLast(8000))
            return true;

        if (player.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
        {
            EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, EEffect.Disease));
        }


        player.Health = player.MaxHealth;
        player.Endurance = player.MaxEndurance;
        player.Mana = player.MaxMana;

        player.Out.SendStatusUpdate();
        return true;
		
		
	}
	public override bool WhisperReceive(GameLiving source, string str)
	{
		if (!base.WhisperReceive(source, str)) return false;
		if (!(source is GamePlayer)) return false;
        if (source.InCombatInLast(10000)) return false;
		GamePlayer t = (GamePlayer)source;
		TurnTo(t.X, t.Y);
		switch (str)
		{
			case "event zone":
				switch (t.Realm)
				{
					case ERealm.Albion:
						t.MoveTo(330, 52759, 39528, 4677, 36);
						break;
					case ERealm.Midgard:
						t.MoveTo(334, 52160, 39862, 5472, 46);
						break;
					case ERealm.Hibernia:
						t.MoveTo(335, 52836, 40401, 4672, 441);
						break;
				}
				break;
			default: break;
		}
		return true;
	}
	private void SendReply(GamePlayer target, string msg)
	{
		target.Client.Out.SendMessage(
			msg,
			EChatType.CT_Say, EChatLoc.CL_PopupWindow);
	}

}


public class LordOfBattleBrain : StandardMobBrain {

    public int timeBeforeRez = 3000; //3 seconds

    Dictionary<GamePlayer, long> playersToRez;
    List<GamePlayer> playersToKill;

    public override void Think()
    {
        if (playersToRez == null)
            playersToRez = new Dictionary<GamePlayer, long>();


        if (playersToKill == null)
            playersToKill = new List<GamePlayer>();

        if (Body.Flags.HasFlag(ENpcFlags.GHOST))
            return;

        foreach(GamePlayer player in Body.GetPlayersInRadius(7000))
        {
            playersToKill.Add(player);
        }

        foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
        {
            if (!player.IsAlive && !playersToRez.ContainsKey(player))
            {
                playersToRez.Add(player, GameLoopMgr.GameLoopTime);
            }

            if (player.effectListComponent.ContainsEffectForEffectType(EEffect.ResurrectionIllness))
            {
                EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, EEffect.ResurrectionIllness));
            }

            if (player.effectListComponent.ContainsEffectForEffectType(EEffect.RvrResurrectionIllness))
            {
                EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, EEffect.RvrResurrectionIllness));
            }

            if(playersToKill.Contains(player))
                playersToKill.Remove(player);
        }

        foreach (GamePlayer deadPlayer in playersToRez.Keys)
        {
            if(playersToRez[deadPlayer] + timeBeforeRez <= GameLoopMgr.GameLoopTime)
            {
                deadPlayer.Health = deadPlayer.MaxHealth;
                deadPlayer.Mana = deadPlayer.MaxMana;
                deadPlayer.Endurance = deadPlayer.MaxEndurance;
                deadPlayer.MoveTo(Body.CurrentRegionID, Body.X, Body.Y+100, Body.Z,
                              Body.Heading);


                deadPlayer.StopReleaseTimer();
                deadPlayer.Out.SendPlayerRevive(deadPlayer);
                deadPlayer.Out.SendStatusUpdate();
                deadPlayer.Out.SendMessage("Mordred has found your soul worthy of resurrection!",
                                       EChatType.CT_System, EChatLoc.CL_SystemWindow);
                deadPlayer.Notify(GamePlayerEvent.Revive, deadPlayer);

                CoreRogMgr.GenerateROG(deadPlayer, true);

                playersToRez.Remove(deadPlayer);
            }
        }

        foreach (GamePlayer player in playersToKill)
        {
            player.MoveTo(Body.CurrentRegionID, Body.X + 100, Body.Y, Body.Z,
                              Body.Heading);
            player.Client.Out.SendMessage("Cowardice is not appreciated in this arena.",
                                       EChatType.CT_Important, EChatLoc.CL_SystemWindow);
        }

        playersToKill.Clear();
    }
}