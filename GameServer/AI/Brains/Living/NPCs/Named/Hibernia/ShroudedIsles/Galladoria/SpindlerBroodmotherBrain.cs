using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

#region Spindler Broodmother
public class SpindlerBroodmotherBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SpindlerBroodmotherBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
    }
    
    public static bool Spawn_Splinders = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            Body.Health = Body.MaxHealth;
            Spawn_Splinders = false;
            StartCastMezz = false;
            CanCast = false;
            RandomTarget = null;
            TeleportTarget = null;
            IsTargetTeleported = false;
            if(Port_Enemys.Count>0)
            {
                Port_Enemys.Clear();
            }
            if (Enemys_To_Mezz.Count > 0)
            {
                Enemys_To_Mezz.Clear();
            }
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is SBAddsBrain && npc != null && npc.IsAlive)
                    {
                        npc.RemoveFromWorld();
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if(Spawn_Splinders==false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnSplinder), 10000);
                Spawn_Splinders = true;
            }
            if (StartCastMezz== false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomTarget), Util.Random(20000, 30000));
                StartCastMezz = true;
            }
            if (Util.Chance(10))
            {
                if (IsTargetTeleported == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickTeleportPlayer), Util.Random(25000, 45000));
                    IsTargetTeleported = true;
                }
            }
        }
        base.Think();
    }
    public int SpawnSplinder(EcsGameTimer timer)
    {
        if (HasAggro && Body.IsAlive)
        {
            for (int i = 0; i < Util.Random(1, 2); i++)
            {
                SBAdds Add = new SBAdds();
                Add.X = Body.X + Util.Random(-50, 80);
                Add.Y = Body.Y + Util.Random(-50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetSpawnSplinder), Util.Random(15000,25000));
        }
        return 0;
    }
    public int ResetSpawnSplinder(EcsGameTimer timer)
    {
        Spawn_Splinders = false;
        return 0;
    }
    #region broodmother mezz
    public static bool CanCast = false;
    public static bool StartCastMezz = false;
    public static GamePlayer randomtarget = null;
    public static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    List<GamePlayer> Enemys_To_Mezz = new List<GamePlayer>();
    public int PickRandomTarget(EcsGameTimer timer)
    {
        if (HasAggro)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!Enemys_To_Mezz.Contains(player))
                        {
                            Enemys_To_Mezz.Add(player);
                        }
                    }
                }
            }
            if (Enemys_To_Mezz.Count > 0)
            {
                if (CanCast == false)
                {
                    GamePlayer Target = (GamePlayer)Enemys_To_Mezz[Util.Random(0, Enemys_To_Mezz.Count - 1)];//pick random target from list
                    RandomTarget = Target;//set random target to static RandomTarget
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastMezz), 3000);
                    CanCast = true;
                }
            }
        }
        return 0;
    }
    public int CastMezz(EcsGameTimer timer)
    {
        if (HasAggro && RandomTarget != null)
        {
            GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
            if (RandomTarget != null && RandomTarget.IsAlive)
            {
                Body.TargetObject = RandomTarget;
                Body.TurnTo(RandomTarget);
                Body.CastSpell(BossMezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetMezz), 5000);
        }
        return 0;
    }
    public int ResetMezz(EcsGameTimer timer)
    {
        RandomTarget = null;
        CanCast = false;
        StartCastMezz = false;
        return 0;
    }
    #endregion
    #region Pick player to port
    public static bool IsTargetTeleported = false;
    public static GamePlayer teleporttarget = null;
    public static GamePlayer TeleportTarget
    {
        get { return teleporttarget; }
        set { teleporttarget = value; }
    }
    List<GamePlayer> Port_Enemys = new List<GamePlayer>();
    public int PickTeleportPlayer(EcsGameTimer timer)
    {
        if (Body.IsAlive && HasAggro)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!Port_Enemys.Contains(player))
                        {
                            if (player != Body.TargetObject)
                            {
                                Port_Enemys.Add(player);
                            }
                        }
                    }
                }
            }
            if (Port_Enemys.Count == 0)
            {
                TeleportTarget = null;//reset random target to null
                IsTargetTeleported = false;
            }
            else
            {
                if (Port_Enemys.Count > 0)
                {
                    GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
                    TeleportTarget = Target;
                    if (TeleportTarget.IsAlive && TeleportTarget != null)
                    {
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayer), 3000);
                    }
                }
            }
        }
        return 0;
    }
    public int TeleportPlayer(EcsGameTimer timer)
    {
        if (TeleportTarget.IsAlive && TeleportTarget != null && HasAggro)
        {
            TeleportTarget.MoveTo(Body.CurrentRegionID, 21115, 53483, 11286, 2100);
            Port_Enemys.Remove(TeleportTarget);
            TeleportTarget = null;//reset random target to null
            IsTargetTeleported = false;
        }
        return 0;
    }
    #endregion

    protected Spell m_BossmezSpell;
    protected Spell BossMezz
    {
        get
        {
            if (m_BossmezSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 5376;
                spell.Icon = 5376;
                spell.TooltipId = 5376;
                spell.Name = "Mesmerized";
                spell.Range = 1500;
                spell.Radius = 300;
                spell.SpellID = 11716;
                spell.Duration = 60;
                spell.Target = "Enemy";
                spell.Type = "Mesmerize";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Spirit; //Spirit DMG Type
                m_BossmezSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BossmezSpell);
            }
            return m_BossmezSpell;
        }
    }
}
#endregion Spindler Broodmother

#region Spindler Broodmother adds
public class SBAddsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SBAddsBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
    }

    public override void Think()
    {
        Body.IsWorthReward = false;
        foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
        {
            if (player != null && player.IsAlive)
            {
                if (player.PlayerClass.ID is 48 or 47 or 42 or 46) //bard,druid,menta,warden
                {
                    if (Body.TargetObject != player)
                    {
                        if (!AggroTable.ContainsKey(player))
                            AddToAggroList(player, 400);
                    }
                }
                else
                {
                    if (!AggroTable.ContainsKey(player))
                        AddToAggroList(player, 10);
                }
            }
        }

        base.Think();
    }
}
#endregion Spindler Broodmother adds

#region Spindler Broodmother post-death adds
public class SBDeadAddsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SBDeadAddsBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Spindler Broodmother post-death adds