using System;
using System.Collections.Generic;
using Core.Events;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class MoranTheMightyBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static bool _aggroStart = true;
    
    public MoranTheMightyBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }

    public override void Think()
    {
        if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
        {
            Body.Health = Body.MaxHealth;
            Body.ReturnToSpawnPoint(NpcMovementComponent.DEFAULT_WALK_SPEED);
        }

        if (HasAggro && Body.InCombat)
        {
            if (Body.TargetObject != null)
            {
                if (_aggroStart)
                {
                    foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
                    {
                        foreach (GamePlayer player in Body.GetPlayersInRadius(2250))
                        {
                            npc.StartAttack(player);
                        }
                    }

                    _aggroStart = false;
                }
                else
                {
                    // chance to teleport a random player to another mob camp 
                    if (Util.Chance(5) && Body.HealthPercent <= 50)
                    {
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayerAway), 5000);
                    }
                }
            }
        }
        else
        {
            _aggroStart = true;
        }

        base.Think();
    }

    public int TeleportPlayerAway(EcsGameTimer timer)
    {
        string gender;
        foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
        {
            if (player == null)
                return 0;

            List<GamePlayer> portPlayer = new List<GamePlayer>();
            portPlayer.Add(player);
            int ranPlayer = Util.Random(0, portPlayer.Count - 1);
            
            if (player.IsAlive && ranPlayer >= 0)
            {
                switch(portPlayer[ranPlayer].Gender)
                {
                    case EGender.Female:
                        gender = "her";
                        break;
                    case EGender.Male:
                        gender = "him";
                        break;
                    case EGender.Neutral:
                        gender = "it";
                        break;
                    default:
                        gender = "it";
                        break;
                }
                
                switch (Util.Random(1, 3))
                {
                    case 1:
                        BroadcastMessage(String.Format("{0} picked up {1} on gust of winds and tossed {2} away! ", Body.Name, portPlayer[ranPlayer].Name, gender));
                        portPlayer[ranPlayer].Out.SendSpellEffectAnimation(portPlayer[ranPlayer], portPlayer[ranPlayer], 1735, 0, false, 1);
                        portPlayer[ranPlayer].MoveTo(1, 401943, 753091, 222, 3499);
                        foreach (GameNpc npc in portPlayer[ranPlayer].GetNPCsInRadius(2000))
                        {
                            npc.StartAttack(portPlayer[ranPlayer]);
                        }
                        portPlayer.Clear();
                        break;
                    case 2:
                        BroadcastMessage(String.Format("{0} picked up {1} on gust of winds and tossed {2} away! ", Body.Name, portPlayer[ranPlayer].Name, gender));
                        portPlayer[ranPlayer].Out.SendSpellEffectAnimation(portPlayer[ranPlayer], portPlayer[ranPlayer], 1735, 0, false, 1);
                        portPlayer[ranPlayer].MoveTo(1, 406787, 749150, 213, 3926);
                        foreach (GameNpc npc in portPlayer[ranPlayer].GetNPCsInRadius(2000))
                        {
                            npc.StartAttack(portPlayer[ranPlayer]);
                        }
                        portPlayer.Clear();
                        break;
                    case 3:
                        BroadcastMessage(String.Format("{0} picked up {1} on gust of winds and tossed {2} away! ", Body.Name, portPlayer[ranPlayer].Name, gender));
                        portPlayer[ranPlayer].Out.SendSpellEffectAnimation(portPlayer[ranPlayer], portPlayer[ranPlayer], 1735, 0, false, 1);
                        portPlayer[ranPlayer].MoveTo(1, 401061, 755882, 469, 3050);
                        foreach (GameNpc npc in portPlayer[ranPlayer].GetNPCsInRadius(2000))
                        {
                            npc.StartAttack(portPlayer[ranPlayer]);
                        }
                        portPlayer.Clear();
                        break;
                    default:
                        break;
                }
            }
            portPlayer.Clear();
        }
        return 0;
    }

    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        base.Notify(e, sender, args);
    }
}