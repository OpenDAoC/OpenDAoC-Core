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
	        if (player.InCombatInLast(10000)) return false;
	        TurnTo(player.X, player.Y);
	        
	        foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(player.CurrentRegionID))
	        {
		        if (npc.Brain is LordOfBossBrain || npc.Name.Contains("Council") || npc.Name.Contains("isolationist") || npc.Name.Contains("muryan"))
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
	        
	        player.Out.SendMessage("Greetings, " + player.CharacterClass.Name + ".\n\n" + "I hope you're up for a challenge.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);

	        switch (player.Realm)
	        {
		        case eRealm._FirstPlayerRealm:
			        player.Out.SendMessage("I have some minions from [Caer Sidi] ready for you..", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			        break;
		        case eRealm.Midgard:
			        player.Out.SendMessage("I have some minions from [Tuscaren Glacier] ready for you..", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			        break;
		        case eRealm.Hibernia:
			        player.Out.SendMessage("I have some minions from [Galladoria] ready for you..", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			        break;
	        }
	        
	        player.Out.SendMessage("..as well as many demons from [Darkness Falls].", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
	        player.Out.SendMessage("If you're not ready, I can also port you [back].", eChatType.CT_Say, eChatLoc.CL_PopupWindow);

	        
            return true;
			
			
		}
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!base.WhisperReceive(source, str)) return false;
			if (!(source is GamePlayer)) return false;
            if (source.InCombatInLast(10000) || source.Group != null && source.Group.Leader != source) return false;
			GamePlayer t = (GamePlayer)source;
			TurnTo(t.X, t.Y);
			switch (str.ToLower())
			{
				case "back":
					switch (t.Realm)
					{
						case eRealm.Albion:
							t.MoveTo(1, 560365, 511888, 2280, 66);
							break;
						case eRealm.Midgard:
							t.MoveTo(100, 804750, 723986, 4680, 671);
							break;
						case eRealm.Hibernia:
							t.MoveTo(200, 345684, 490996, 1071, 900);
							break;
					}
					break;
				
				#region Caer Sidi
				case "caer sidi":
					if (t.Realm != eRealm.Albion) return false;
					t.Out.SendMessage("I can summon the following bosses from Caer Sidi:\n\n" +
					                  "1. [Skeletal Sacristan]\n" +
					                  "2. [Spectral Provisioner]\n" +
					                  "3. [Lich Lord Ilron]\n" 
					                  // "4. [Bane of Hope]\n"
						, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					break;

				case "skeletal sacristan":
					if (t.Realm != eRealm.Albion) return false;
					SummonBoss(t,"DOL.GS.Scripts.SkeletalSacristan");
					break;
				
				case "spectral provisioner":
					if (t.Realm != eRealm.Albion) return false;
					SummonBoss(t,"DOL.GS.Scripts.SpectralProvisioner");
					break;
				
				case "lich lord ilron":
					if (t.Realm != eRealm.Albion) return false;
					SummonBoss(t,"DOL.GS.Scripts.LichLordIlron");
					break;
				
				case "bane of hope":
					if (t.Realm != eRealm.Albion) return false;
					SummonBoss(t,"DOL.GS.Scripts.BaneOfHope");
					break;
				#endregion
				
				#region Galladoria
				case "galladoria":
					if (t.Realm != eRealm.Hibernia) return false;
					t.Out.SendMessage("I can summon the following bosses from Galladoria:\n\n" +
					                  "1. [Easmarach]\n" +
					                  "2. [Organic Energy Mechanism]\n" +
					                  "3. [Giant Sporite Cluster]\n" +
					                  "4. [Conservator]\n" +
					                  "5. [Xaga]\n" +
					                  "6. [Spindler Broodmother]\n" +
					                  "7. [Olcasar Geomancer]\n"
						, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					break;

				case "easmarach":
					if (t.Realm != eRealm.Hibernia) return false;
					SummonBoss(t,"DOL.GS.Easmarach");
					break;
				
				case "organic energy mechanism":
					if (t.Realm != eRealm.Hibernia) return false;
					SummonBoss(t,"DOL.GS.OrganicEnergyMechanism");
					break;
				
				case "giant sporite cluster":
					if (t.Realm != eRealm.Hibernia) return false;
					SummonBoss(t,"DOL.GS.GiantSporiteCluster");
					break;
				
				case "conservator":
					if (t.Realm != eRealm.Hibernia) return false;
					SummonBoss(t,"DOL.GS.Conservator");
					break;
				
				case "xaga":
					if (t.Realm != eRealm.Hibernia) return false;
					SummonBoss(t,"DOL.GS.Xaga");
					break;
				
				case "spindler broodmother":
					if (t.Realm != eRealm.Hibernia) return false;
					SummonBoss(t,"DOL.GS.SpindlerBroodmother");
					break;
				
				case "olcasar geomancer":
					if (t.Realm != eRealm.Hibernia) return false;
					SummonBoss(t,"DOL.GS.OlcasarGeomancer");
					break;
				#endregion
				
				#region Darkness Falls
				case "darkness falls":
					t.Out.SendMessage("Demons are tricky to capture, try again later."
						, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					break;
				
				#endregion
					
				case "reset":
					if (source.InCombatInLast(10000)) return false;
					foreach (GameNPC mob in WorldMgr.GetNPCsFromRegion(t.CurrentRegionID))
					{
						if (mob.Brain is LordOfBossBrain || mob.Name.Contains("Council") || mob.Name.Contains("isolationist") || mob.Name.Contains("muryan"))
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
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(player.CurrentRegionID))
			{
				if (npc.Brain is LordOfBossBrain || npc.Name.Contains("Council") || npc.Name.Contains("isolationist") || npc.Name.Contains("muryan"))
					continue;
				player.Out.SendMessage("You have already summoned a boss!.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				return;
			}
			
			
			//Create a new mob
			GameNPC mob = new GameNPC();
			mob = (GameNPC) Assembly.GetAssembly(typeof(GameServer)).CreateInstance(BossClass, false);
			Console.WriteLine("Summoned " + BossClass);

			if (mob == null)
				Console.WriteLine("Error loading mob " + BossClass);

			//Fill the object variables
				mob.X = 34885;
				mob.Y = 35347;
				mob.Z = 19153;
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
