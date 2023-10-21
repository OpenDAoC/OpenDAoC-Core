using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Olcasar Geomancer adds
public class OlcasarGeomancerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public OlcasarGeomancerBrain()
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
    public static bool spawnadds = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            spawnadds = false;
            CanCast2 = false;
            StartCastRoot = false;
            CanCastAoeSnare = false;
            RandomTarget2 = null;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
                {
                    if (npc.Brain is OlcasarAddsBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
                RemoveAdds = true;
            }
        }
        if (Body.InCombatInLast(30 * 1000) == false && Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (StartCastRoot == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomTarget2), Util.Random(25000, 35000));
                StartCastRoot = true;
            }
            if(spawnadds ==false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastEffectBubble), 25000);
                spawnadds = true;
            }
            if (Util.Chance(15))
            {
                if (LivingHasEffect(Body.TargetObject as GameLiving, OGDS) == false)
                {
                    Body.CastSpell(OGDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
            if(CanCastAoeSnare == false &&  Body.HealthPercent <= 80)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastAoeSnare), 5000);
                CanCastAoeSnare = true;
            }
        }
        base.Think();
    }
    #region Cast root on random target
    public static bool CanCast2 = false;
    public static bool StartCastRoot = false;
    public static GamePlayer randomtarget2 = null;
    public static GamePlayer RandomTarget2
    {
        get { return randomtarget2; }
        set { randomtarget2 = value; }
    }
    List<GamePlayer> Enemys_To_Root = new List<GamePlayer>();
    public int PickRandomTarget2(EcsGameTimer timer)
    {
        if (HasAggro)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!Enemys_To_Root.Contains(player))
                        {
                            Enemys_To_Root.Add(player);
                        }
                    }
                }
            }
            if (Enemys_To_Root.Count > 0)
            {
                if (CanCast2 == false)
                {
                    GamePlayer Target = (GamePlayer)Enemys_To_Root[Util.Random(0, Enemys_To_Root.Count - 1)];//pick random target from list
                    RandomTarget2 = Target;//set random target to static RandomTarget
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastRoot), 2000);
                    CanCast2 = true;
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
                Body.CastSpell(OGRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetRoot), 5000);
        }
        return 0;
    }
    public int ResetRoot(EcsGameTimer timer)
    {
        RandomTarget2 = null;
        CanCast2 = false;
        StartCastRoot = false;
        return 0;
    }
    #endregion
    public int CastEffectBubble(EcsGameTimer timer)
    {
        Body.CastSpell(OGBubbleEffect, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        BroadcastMessage(String.Format("Olcasar tears off a chunk of himself and tosses it to the ground."));
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Spawn), 2000);
        return 0;
    }
    public int Spawn(EcsGameTimer timer)
    {
        if (Body.IsAlive && HasAggro && Body.TargetObject != null)
        {
            OlcasarAdds Add = new OlcasarAdds();
            Add.X = Body.X + Util.Random(-50, 80);
            Add.Y = Body.Y + Util.Random(-50, 80);
            Add.Z = Body.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.AddToWorld();             
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetSpawn), Util.Random(45000, 60000));
        }
        return 0;
    }
    public int ResetSpawn(EcsGameTimer timer)
    {
        spawnadds = false;
        return 0;
    }

    public static bool CanCastAoeSnare = false;
    public int CastAoeSnare(EcsGameTimer timer)
    {
        if (Body.IsAlive && HasAggro)
        {
            Body.CastSpell(OGAoeSnare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetAoeSnare), Util.Random(45000, 60000));
        }
        return 0;
    }
    public int ResetAoeSnare(EcsGameTimer timer)
    {
        CanCastAoeSnare = false;
        return 0;
    }
    #region Spells
    private Spell m_OGDS;
    private Spell OGDS
    {
        get
        {
            if (m_OGDS == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 30;
                spell.ClientEffect = 57;
                spell.Icon = 57;
                spell.Damage = 20;
                spell.Duration = 30;
                spell.Name = "Geomancer Damage Shield";
                spell.TooltipId = 57;
                spell.SpellID = 11717;
                spell.Target = "Self";
                spell.Type = "DamageShield";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Heat;
                m_OGDS = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGDS);
            }
            return m_OGDS;
        }
    }
    private Spell m_OGRoot;
    private Spell OGRoot
    {
        get
        {
            if (m_OGRoot == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 60;
                spell.ClientEffect = 5089;
                spell.Icon = 5089;
                spell.Duration = 60;
                spell.Value = 99;
                spell.Name = "Geomancer Root";
                spell.TooltipId = 5089;
                spell.SpellID = 11718;
                spell.Target = "Enemy";
                spell.Type = "SpeedDecrease";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Matter;
                m_OGRoot = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGRoot);
            }
            return m_OGRoot;
        }
    }
    private Spell m_OGAoeSnare;
    private Spell OGAoeSnare
    {
        get
        {
            if (m_OGAoeSnare == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 77;
                spell.Icon = 77;
                spell.Duration = 60;
                spell.Value = 60;
                spell.Radius = 2500;
                spell.Range = 0;
                spell.Name = "Olcasar Snare";
                spell.TooltipId = 77;
                spell.SpellID = 11862;
                spell.Target = "Enemy";
                spell.Type = ESpellType.SpeedDecrease.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Matter;
                m_OGAoeSnare = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGAoeSnare);
            }
            return m_OGAoeSnare;
        }
    }
    private Spell m_OGBubbleEffect;
    private Spell OGBubbleEffect
    {
        get
        {
            if (m_OGBubbleEffect == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 5126;
                spell.Icon = 5126;
                spell.Value = 1;
                spell.Name = "Olcasar Tear";
                spell.TooltipId = 5126;
                spell.SpellID = 11861;
                spell.Target = "Self";
                spell.Type = "Heal";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_OGBubbleEffect = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGBubbleEffect);
            }
            return m_OGBubbleEffect;
        }
    }
    #endregion
}
#endregion Olcasar Geomancer

#region Olcasar Geomancer adds
public class OlcasarAddsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public OlcasarAddsBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
    }
    public override void Think()
    {
        if(HasAggro && Body.TargetObject != null)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity) && target != null && target.IsAlive)
            {
                Body.CastSpell(addstun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
        {
            if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
            {
                if (player.PlayerClass.ID is 48 or 47 or 42 or 46) //bard,druid,menta,warden
                {
                    if (Body.TargetObject != player)
                    {
                        AddToAggroList(player, 200);
                    }
                }
                else
                {
                    Body.TargetObject = player;
                    AddToAggroList(player, 200);
                }
            }
        }
        base.Think();
    }
    private Spell m_addstun;
    private Spell addstun
    {
        get
        {
            if (m_addstun == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 2132;
                spell.Icon = 2132;
                spell.Duration = 9;
                spell.Range = 500;
                spell.Name = "Stun";
                spell.Description = "Stuns the target for 9 seconds.";
                spell.Message1 = "You cannot move!";
                spell.Message2 = "{0} cannot seem to move!";
                spell.Message3 = "You recover from the stun.";
                spell.Message4 = "{0} recovers from the stun.";
                spell.TooltipId = 2132;
                spell.SpellID = 11864;
                spell.Target = "Enemy";
                spell.Type = "StyleStun";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Body;
                m_addstun = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_addstun);
            }
            return m_addstun;
        }
    }
}
#endregion Olcasar Geomancer adds