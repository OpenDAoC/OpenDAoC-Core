using System;
using System.Collections;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Icelord Hakr
public class IcelordHakrBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public IcelordHakrBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public static bool IsPulled = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsPulled == false)
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.Brain is HakrAddBrain brain && npc.PackageID == "HakrBaf")
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!brain.HasAggro && Body != npc && target != null && target.IsAlive)
                            brain.AddToAggroList(target, 10);
                        IsPulled = true;
                    }
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public void TeleportPlayer()
    {
        if (HakrAdd.IceweaverCount > 0)
        {
            IList enemies = new ArrayList(AggroTable.Keys);
            foreach (GamePlayer player in Body.GetPlayersInRadius(1100))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!AggroTable.ContainsKey(player))
                            AggroTable.Add(player, 1);
                    }
                }
            }
            if (enemies.Count == 0)
                return;
            else
            {
                List<GameLiving> damage_enemies = new List<GameLiving>();
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null)
                        continue;
                    if (!(enemies[i] is GameLiving))
                        continue;
                    if (!(enemies[i] as GameLiving).IsAlive)
                        continue;
                    GameLiving living = null;
                    living = enemies[i] as GameLiving;
                    if (living.IsVisibleTo(Body) && Body.TargetInView && living is GamePlayer)
                    {
                        damage_enemies.Add(enemies[i] as GameLiving);
                    }
                }
                if (damage_enemies.Count > 0)
                {
                    GamePlayer PortTarget = (GamePlayer) damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
                    if (PortTarget.IsVisibleTo(Body) && Body.TargetInView)
                    {
                        PortTarget.MoveTo(Body.CurrentRegionID, Body.X + Util.Random(-50, 50),
                        Body.Y + Util.Random(-50, 50), Body.Z + 220, Body.Heading);
                        BroadcastMessage(String.Format("Icelord Hakr says, '" + PortTarget.Name +" Touchdown! That's a really cool way of putting it!'"));
                        PortTarget = null;
                    }
                }
            }
        }
    }
    public int PortTimer(EcsGameTimer timer)
    {
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DoPortTimer), 2000);
        return 0;
    }
    public int DoPortTimer(EcsGameTimer timer)
    {
        TeleportPlayer();
        spam_teleport = false;
        return 0;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool spam_teleport = false;
    public static bool spam_message1 = false;
    public override void Think()
    {
        if (HakrAdd.IceweaverCount == 0 && spam_message1 == false && Body.IsAlive)
        {
            BroadcastMessage(String.Format("Magic barrier fades away from Icelord Hakr!"));
            spam_message1 = true;
        }
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsPulled = false;
            spam_message1 = false;
            spam_teleport = false;
        }
        if (HasAggro)
        {
            if (spam_teleport == false && Body.TargetObject != null && HakrAdd.IceweaverCount > 0)
            {
                int rand = Util.Random(10000, 20000);
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PortTimer), rand);
                spam_teleport = true;
            }
        }
        base.Think();
    }
}
#endregion Icelord Hakr

#region Hakr snake adds
public class HakrAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public HakrAddBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    public static bool IsPulled = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsPulled == false)
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.Brain is HakrAddBrain brain && npc.PackageID == "HakrBaf")
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!brain.HasAggro && Body != npc && target != null && target.IsAlive)
                            brain.AddToAggroList(target, 10);
                        IsPulled = true;
                    }
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            IsPulled = false;
        }
        if (HasAggro)
        {
            if (Body.TargetObject != null)
            {
                if (Body.TargetObject.IsWithinRadius(Body, Body.AttackRange))
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
                    {
                        if (Util.Chance(25))
                            Body.CastSpell(IceweaverPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                }
            }
        }
        base.Think();
    }
    public Spell m_IceweaverPoison;
    public Spell IceweaverPoison
    {
        get
        {
            if (m_IceweaverPoison == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.ClientEffect = 3411;
                spell.TooltipId = 3411;
                spell.Icon = 3411;
                spell.Damage = 100;
                spell.Duration = 30;
                spell.Name = "Iceweaver's Poison";
                spell.Description = "Inflicts 100 damage to the target every 3 sec for 30 seconds";
                spell.Message1 = "You are wracked with pain!";
                spell.Message2 = "{0} is wracked with pain!";
                spell.Message3 = "You look healthy again.";
                spell.Message4 = "{0} looks healthy again.";
                spell.Frequency = 30;
                spell.Range = 400;
                spell.SpellID = 11746;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DamageOverTime.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Body;
                m_IceweaverPoison = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IceweaverPoison);
            }
            return m_IceweaverPoison;
        }
    }
}
#endregion