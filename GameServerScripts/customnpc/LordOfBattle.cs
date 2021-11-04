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
            Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
            SetOwnBrain(new LordOfBattleBrain());

            return base.AddToWorld(); // Finish up and add him to the world.
        }   

    }

    public class LordOfBattleBrain : StandardMobBrain {

        public int timeBeforeRez = 3000; //3 seconds

        Dictionary<GamePlayer, long> playersToRez;

        public override void Think()
        {
            if (playersToRez == null)
            {
                playersToRez = new Dictionary<GamePlayer, long>();
            }

            foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
            {
                if (!player.IsAlive && !playersToRez.ContainsKey(player))
                {
                    playersToRez.Add(player, GameLoop.GameLoopTime);
                } else
                {
                    if (player.effectListComponent.ContainsEffectForEffectType(eEffect.ResurrectionIllness))
                    {
                        EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, eEffect.ResurrectionIllness));
                    }

                    if (player.effectListComponent.ContainsEffectForEffectType(eEffect.RvrResurrectionIllness))
                    {
                        EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(player, eEffect.RvrResurrectionIllness));
                    }
                    player.Out.SendStatusUpdate();
                }
            }

            foreach (GamePlayer deadPlayer in playersToRez.Keys)
            {
                if(playersToRez[deadPlayer] + timeBeforeRez <= GameLoop.GameLoopTime)
                {
                    deadPlayer.Health = deadPlayer.MaxHealth;
                    deadPlayer.Mana = deadPlayer.MaxMana;
                    deadPlayer.Endurance = deadPlayer.MaxEndurance;
                    deadPlayer.MoveTo(Body.CurrentRegionID, Body.X, Body.Y, Body.Z,
                                  Body.Heading);


                    deadPlayer.StopReleaseTimer();
                    deadPlayer.Out.SendPlayerRevive(deadPlayer);
                    deadPlayer.Out.SendStatusUpdate();
                    deadPlayer.Out.SendMessage("Mordred has found your soul worthy of resurrection!",
                                           eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    deadPlayer.Notify(GamePlayerEvent.Revive, deadPlayer);

                    playersToRez.Remove(deadPlayer);
                }
            }
        }
    }
}
