using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Elder Council Guthlac
public class ElderCouncilGuthlacBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ElderCouncilGuthlacBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool message1 = false;
    public static bool IsBombUp = false;
    public static GameLiving randomtarget = null;
    public static GameLiving RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    List<GamePlayer> PlayersToDD = new List<GamePlayer>();

    #region Root && debuff
    public static bool CanCast = false;
    public static bool StartCastRoot = false;
    public static GameLiving randomtarget2 = null;
    public static GameLiving RandomTarget2
    {
        get { return randomtarget2; }
        set { randomtarget2 = value; }
    }
    List<GamePlayer> Enemys_To_Root = new List<GamePlayer>();

    public int PickRandomTarget(EcsGameTimer timer)
    {
        if (HasAggro)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
            {
                if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
                {
                    if (!Enemys_To_Root.Contains(player) && player != Body.TargetObject)
                        Enemys_To_Root.Add(player);
                }
            }
            if (Enemys_To_Root.Count > 0)
            {
                if (CanCast == false)
                {
                    GamePlayer Target = (GamePlayer)Enemys_To_Root[Util.Random(0, Enemys_To_Root.Count - 1)];//pick random target from list
                    RandomTarget2 = Target;//set random target to static RandomTarget
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastRoot), 1000);
                    CanCast = true;
                }
            }
        }
        return 0;
    }
    public int CastRoot(EcsGameTimer timer)
    {
        if (HasAggro && RandomTarget2 != null)
        {
            GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
            if (RandomTarget2 != null && RandomTarget2.IsAlive)
            {
                Body.TargetObject = RandomTarget2;
                Body.TurnTo(RandomTarget2);
                Body.CastSpell(GuthlacRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
                Body.CastSpell(DebuffDQ, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }
            if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetRoot), 5000);
        }
        return 0;
    }
    public int ResetRoot(EcsGameTimer timer)
    {
        Enemys_To_Root.Clear();
        RandomTarget2 = null;
        CanCast = false;
        StartCastRoot = false;
        return 0;
    }
    #endregion

    public static bool IsPulled2 = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsPulled2 == false)
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.Brain is ElderCouncilBirghirBrain)
                    {
                        AddAggroListTo(npc.Brain as ElderCouncilBirghirBrain);
                        IsPulled2 = true;
                    }
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsPulled2 = false;
            RandomTarget = null;
            FrozenBomb.FrozenBombCount = 0;
            message1 = false;
            IsBombUp = false;
            StartCastRoot = false;
            CanCast = false;
            RandomTarget2 = null;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is FrozenBombBrain)
                            npc.Die(Body);
                    }
                }
                RemoveAdds = true;
            }
        }

        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            Body.Health = Body.MaxHealth;

        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if(!StartCastRoot)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomTarget), Util.Random(35000, 45000));
                StartCastRoot = true;
            }
            foreach (GamePlayer player in Body.GetPlayersInRadius(4500))
            {
                if (player == null) break;
                if (player.IsAlive)
                {
                    if (!PlayersToDD.Contains(player))
                        PlayersToDD.Add(player);
                }
            }
            if (IsBombUp == false && FrozenBomb.FrozenBombCount == 0)
            {
                if (PlayersToDD.Count > 0)
                {
                    GamePlayer ptarget = PlayersToDD[Util.Random(0, PlayersToDD.Count - 1)];
                    RandomTarget = ptarget;
                }
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnBombTimer), Util.Random(35000, 60000)); //spawn frozen bomb every 35s-60s
                IsBombUp = true;
            }
            if (message1 == false)
            {
                BroadcastMessage(String.Format(Body.Name +" says, 'I didn't think it was possible that our home could fall victim to an invasion!" +
                    " The Ice Lords were right! We should have wiped out all dangerous creatures on this island! And we're going to do that today!'"));
                message1 = true;
            }
            if(!Body.IsCasting)
                Body.CastSpell(Icelord_dd, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
        }
        base.Think();
    }

    #region Spawn Frost Bomb
    public int SpawnBombTimer(EcsGameTimer timer)
    {
        if (FrozenBomb.FrozenBombCount == 0)
            SpawnFrozenBomb();

        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetBomb), 5000);
        return 0;
    }
    public int ResetBomb(EcsGameTimer timer)
    {
        RandomTarget = null;
        IsBombUp = false;
        return 0;
    }
    public void SpawnFrozenBomb()
    {
        FrozenBomb npc = new FrozenBomb();
        npc.Name = "Ice Spike";
        if (RandomTarget != null)
        {
            npc.X = RandomTarget.X;
            npc.Y = RandomTarget.Y;
            npc.Z = RandomTarget.Z;
            BroadcastMessage(String.Format(npc.Name + " appears on " + RandomTarget.Name +", It's unstable form will soon errupt."));
        }
        else
        {
            npc.X = Body.X;
            npc.Y = Body.Y;
            npc.Z = Body.Z;
            BroadcastMessage(String.Format(npc.Name + " appears nearby, It's unstable form will soon errupt."));
        }

        npc.RespawnInterval = -1;
        npc.Heading = Body.Heading;
        npc.CurrentRegion = Body.CurrentRegion;
        npc.AddToWorld();
    }
    #endregion

    #region Spells
    private Spell m_DebuffDQ;
    private Spell DebuffDQ
    {
        get
        {
            if (m_DebuffDQ == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.Duration = 60;
                spell.Value = 80;
                spell.ClientEffect = 2627;
                spell.Icon = 2627;
                spell.TooltipId = 2627;
                spell.Name = "Greater Curse of Blindness";
                spell.Range = 1500;
                spell.Radius = 350;
                spell.SpellID = 11932;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DexterityQuicknessDebuff.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_DebuffDQ = new Spell(spell, 60);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DebuffDQ);
            }
            return m_DebuffDQ;
        }
    }
    private Spell m_GuthlacRoot;
    private Spell GuthlacRoot
    {
        get
        {
            if (m_GuthlacRoot == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 2678;
                spell.Icon = 2678;
                spell.Duration = 60;
                spell.Value = 99;
                spell.Name = "Root";
                spell.TooltipId = 2678;
                spell.SpellID = 11931;
                spell.Target = "Enemy";
                spell.Type = "SpeedDecrease";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Body;
                m_GuthlacRoot = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GuthlacRoot);
            }
            return m_GuthlacRoot;
        }
    }
    private Spell m_Icelord_dd;
    private Spell Icelord_dd
    {
        get
        {
            if (m_Icelord_dd == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = Util.Random(8,15);
                spell.ClientEffect = 2709;
                spell.Icon = 2709;
                spell.TooltipId = 2709;
                spell.Damage = 550;
                spell.Duration = 30;
                spell.Value = 35;
                spell.Name = "Rune of Mazing";
                spell.Range = 1800;
                spell.SpellID = 11930;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DamageSpeedDecreaseNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Energy;
                m_Icelord_dd = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Icelord_dd);
            }
            return m_Icelord_dd;
        }
    }
    #endregion
}
#endregion Elder Council Guthlac

#region Ice Bomb
public class FrozenBombBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public FrozenBombBrain()
        : base()
    {
        AggroLevel = 0;
        AggroRange = 0;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (Body.IsAlive)
        {
            //FSM.SetCurrentState(eFSMStateType.AGGRO);
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!AggroTable.ContainsKey(player))
                            AggroTable.Add(player, 100);
                    }
                }
            }
        }
        base.Think();
    }
}
#endregion Ice Bomb