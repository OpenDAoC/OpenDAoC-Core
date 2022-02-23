using System;
using DOL.Events;
using DOL.GS.PacketHandler;
using System.Collections.Generic;
using System.Reflection;
using DOL.AI.Brain;
using DOL.GS.API;

namespace DOL.GS {
    public class LordOfBoss : GameTrainingDummy {
       

        public override bool AddToWorld()
        {
            Name = "Mordbro";
            GuildName = "Lord Of Bosses";
            Realm = 0;
            Model = 1903;
            Size = 60;
            Level = 75;
            Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
            SetOwnBrain(new LordOfBossBrain());

            return base.AddToWorld(); // Finish up and add him to the world.
        }

        public override bool Interact(GamePlayer player)
        {
	        bool inFight = false;
	        
	        if (!base.Interact(player)) return false;
	        TurnTo(player.X, player.Y);
	        
	        foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(player.CurrentRegionID))
	        {
		        if (npc.Brain is LordOfBossBrain)
			        continue; 
		        inFight = true;
	        }

	        if (player.Group == null && player.Client.Account.PrivLevel == 1)
	        {
		        player.Out.SendMessage($"This challenge is too big for a lonely {player.CharacterClass.Name}, assemble a group and come back to me! \n I can port you [back] while you find some companions.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
		        return false;
	        }
	        
	        if (player.Group != null && player.Group.Leader != player)
	        {
		        player.Out.SendMessage($"You are not the leader of your group, {player.CharacterClass.Name}. Ask them to come speak with me to start.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
		        return false;
	        }
	        
	        if (inFight)
	        {
		        player.Out.SendMessage($"There's a battle already going on, {player.CharacterClass.Name}. You can't start a new fight yet. \n\n I can also [reset] the arena.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
		        return false;
	        }
	        

	        player.Out.SendMessage("Greetings, " + player.CharacterClass.Name + ".\n\n" + "I hope you're up for a challenge, my minions definitely are. \n Are you ready to [start]?", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
	        
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
				case "back":
					switch (t.Realm)
					{
						case eRealm.Albion:
							t.MoveTo(330, 52759, 39528, 4677, 36);
							break;
						case eRealm.Midgard:
							t.MoveTo(334, 52160, 39862, 5472, 46);
							break;
						case eRealm.Hibernia:
							t.MoveTo(200, 345684, 490996, 1071, 900);
							break;
					}
					break;
				case "start":
					t.Out.SendMessage("I can summon [bane] if you wish.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					break;
				
				case "bane":
					SummonBoss(t,"DOL.GS.Scripts.BaneOfHope");
					break;
					
				case "reset":
					foreach (GameNPC mob in WorldMgr.GetNPCsFromRegion(t.CurrentRegionID))
					{
						if (mob.Brain is LordOfBossBrain)
							continue;
						mob.Delete();
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

		private void SummonBoss(GamePlayer player, string BossClass)
		{
			//Create a new mob

			GameNPC mob = new GameNPC();
			mob = (GameNPC) Assembly.GetAssembly(typeof(GameServer)).CreateInstance(BossClass, false);
			Console.WriteLine("Summoned " + BossClass);

			if (mob == null)
				Console.WriteLine("Error loading mob " + BossClass);

			//Fill the object variables
				mob.X = 32000;
				mob.Y = 33155;
				mob.Z = 16000;
				mob.CurrentRegion = player.CurrentRegion;
				mob.Heading = 2050;

				//Fill the living variables
				mob.Flags |= eFlags.PEACE;
				mob.AddToWorld();
				
				
	    }

		}

	


    public class LordOfBossBrain : StandardMobBrain {

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
