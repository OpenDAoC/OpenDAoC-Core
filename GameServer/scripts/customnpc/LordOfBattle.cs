using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;

namespace DOL.GS {
    public class LordOfBattle : GameTrainingDummy {
       

        public override bool AddToWorld()
        {
            Name = "Mordred";
            GuildName = "Lord Of Battle";
            Realm = 0;
            Model = 1903;
            Size = 250;
            Level = 75;
            Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
            SetOwnBrain(new LordOfBattleBrain());

            return base.AddToWorld(); // Finish up and add him to the world.
        }

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			TurnTo(player.X, player.Y);

			
				player.Out.SendMessage("Greetings, " + player.CharacterClass.Name + ".\n\n" + "If you desire, I can port you back to your realm's [event zone]", eChatType.CT_Say, eChatLoc.CL_PopupWindow);

            if (player.effectListComponent.ContainsEffectForEffectType(eEffect.ResurrectionIllness))
            {
                EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, eEffect.ResurrectionIllness));
            }

            if (player.effectListComponent.ContainsEffectForEffectType(eEffect.RvrResurrectionIllness))
            {
                EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, eEffect.RvrResurrectionIllness));
            }


            if (player.InCombatPvPInLast(8000))
                return true;

            if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Disease))
            {
                EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, eEffect.Disease));
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
						case eRealm.Albion:
							t.MoveTo(330, 52759, 39528, 4677, 36);
							break;
						case eRealm.Midgard:
							t.MoveTo(334, 52160, 39862, 5472, 46);
							break;
						case eRealm.Hibernia:
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
				eChatType.CT_Say, eChatLoc.CL_PopupWindow);
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

            if (Body.Flags.HasFlag(GameNPC.eFlags.GHOST))
                return;

            foreach(GamePlayer player in Body.GetPlayersInRadius(7000))
            {
                playersToKill.Add(player);
            }

            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (!player.IsAlive && !playersToRez.ContainsKey(player))
                {
                    playersToRez.Add(player, GameLoop.GameLoopTime);
                }

                if (player.effectListComponent.ContainsEffectForEffectType(eEffect.ResurrectionIllness))
                {
                    EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, eEffect.ResurrectionIllness));
                }

                if (player.effectListComponent.ContainsEffectForEffectType(eEffect.RvrResurrectionIllness))
                {
                    EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, eEffect.RvrResurrectionIllness));
                }

                if(playersToKill.Contains(player))
                    playersToKill.Remove(player);
            }

            foreach (GamePlayer deadPlayer in playersToRez.Keys)
            {
                if(playersToRez[deadPlayer] + timeBeforeRez <= GameLoop.GameLoopTime)
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
                                           eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    deadPlayer.Notify(GamePlayerEvent.Revive, deadPlayer);

                    AtlasROGManager.GenerateROG(deadPlayer, true);

                    playersToRez.Remove(deadPlayer);
                }
            }

            foreach (GamePlayer player in playersToKill)
            {
                player.MoveTo(Body.CurrentRegionID, Body.X + 100, Body.Y, Body.Z,
                                  Body.Heading);
                player.Client.Out.SendMessage("Cowardice is not appreciated in this arena.",
                                           eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            playersToKill.Clear();
        }
    }
}
