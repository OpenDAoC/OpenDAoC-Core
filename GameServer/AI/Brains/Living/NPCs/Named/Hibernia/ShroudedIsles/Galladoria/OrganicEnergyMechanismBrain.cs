using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Organic Energy Mechanism
public class OrganicEnergyMechanismBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public OrganicEnergyMechanismBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    private bool RemoveAdds = false;
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    #region OEM Dot
    public static bool CanCast = false;
    public static bool StartCastDOT = false;
    public static GamePlayer randomtarget = null;
    public static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    List<GamePlayer> Enemys_To_DOT = new List<GamePlayer>();
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
                        if (!Enemys_To_DOT.Contains(player))
                        {
                            Enemys_To_DOT.Add(player);
                        }
                    }
                }
            }
            if (Enemys_To_DOT.Count > 0)
            {
                if (CanCast == false)
                {
                    GamePlayer Target = (GamePlayer)Enemys_To_DOT[Util.Random(0, Enemys_To_DOT.Count - 1)];//pick random target from list
                    RandomTarget = Target;//set random target to static RandomTarget
                    BroadcastMessage(String.Format(Body.Name + "looks sickly... powerfull magic essense will errupt on " + RandomTarget.Name + "!"));
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDOT), 5000);
                    CanCast = true;
                }
            }
        }
        return 0;
    }
    public int CastDOT(EcsGameTimer timer)
    {
        if (HasAggro && RandomTarget != null)
        {
            GamePlayer oldTarget = (GamePlayer)Body.TargetObject;//old target
            if (RandomTarget != null && RandomTarget.IsAlive)
            {
                Body.TargetObject = RandomTarget;
                Body.TurnTo(RandomTarget);
                Body.CastSpell(OEMpoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDOT), 5000);
        }
        return 0;
    }
    public int ResetDOT(EcsGameTimer timer)
    {
        RandomTarget = null;
        CanCast = false;
        StartCastDOT = false;
        return 0;
    }
    #endregion
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if(ad != null)
        {
            if (Util.Chance(50) && !Body.IsCasting)
                Body.CastSpell(OEMDamageShield, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

            if (Util.Chance(50) && !Body.IsCasting)
                Body.CastSpell(OEMEffect, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        if (Body.InCombatInLast(40 * 1000) == false && Body.InCombatInLast(45 * 1000))
        {
            if(AggroTable.Count>0)
                ClearAggroList();
        }
        if (!CheckProximityAggro())
        {
            Body.Health = Body.MaxHealth;
            RandomTarget = null;
            CanCast = false;
            StartCastDOT = false;
            RandomTarget = null;
            SpawnFeeder = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is EnergyMechanismAddBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.IsAlive)
        {
            RemoveAdds = false;
            //DOT is not classic like, can be anabled if we wish to
            /* if (StartCastDOT == false)
             {
                 new RegionTimer(Body, new RegionTimerCallback(PickRandomTarget), Util.Random(20000, 25000));
                 StartCastDOT = true;
             }*/

            if (SpawnFeeder==false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnFeeders), 10000);
                SpawnFeeder = true;
            }
        }
        base.Think();
    }
    public static bool SpawnFeeder = false;
    public int SpawnFeeders(EcsGameTimer timer) // We define here adds
    {
        if (Body.IsAlive && HasAggro)
        {
            for (int i = 0; i < Util.Random(3, 5); i++)
            {
                EnergyMechanismAdd Add = new EnergyMechanismAdd();
                Add.X = Body.X + Util.Random(-50, 80);
                Add.Y = Body.Y + Util.Random(-50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetSpawnFeeders), Util.Random(15000,25000));
        }
        return 0;
    }
    public int ResetSpawnFeeders(EcsGameTimer timer)
    {
        SpawnFeeder = false;
        return 0;
    }
    #region Spells
    private Spell m_AOE_Poison;
    private Spell OEMpoison
    {
        get
        {
            if (m_AOE_Poison == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 4445;
                spell.Icon = 4445;
                spell.Damage = 200;
                spell.Name = "Essense of World Soul";
                spell.Description = "Inflicts powerfull magic damage to the target, then target dies in painfull agony.";
                spell.Message1 = "You are wracked with pain!";
                spell.Message2 = "{0} is wracked with pain!";
                spell.Message3 = "You look healthy again.";
                spell.Message4 = "{0} looks healthy again.";
                spell.TooltipId = 4445;
                spell.Range = 1800;
                spell.Radius = 600;
                spell.Duration = 50;
                spell.Frequency = 50; //dot tick every 5s
                spell.SpellID = 11700;
                spell.Target = "Enemy";
                spell.Type = "DamageOverTime";
                spell.Uninterruptible = true;
                spell.DamageType = (int) EDamageType.Matter; //Spirit DMG Type
                m_AOE_Poison = new Spell(spell, 50);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AOE_Poison);
            }
            return m_AOE_Poison;
        }
    }
    private Spell m_DamageShield;
    private Spell OEMDamageShield
    {
        get
        {
            if (m_DamageShield == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 35;
                spell.ClientEffect = 11027; //509
                spell.Icon = 11027;
                spell.Damage = 150;
                spell.Name = "Shield of World Soul";
                spell.Message2 = "{0}'s armor becomes sorrounded with powerfull magic.";
                spell.Message4 = "{0}'s powerfull magic wears off.";
                spell.TooltipId = 11027;
                spell.Range = 1800;
                spell.Duration = 35;
                spell.SpellID = 11701;
                spell.Target = "Self";
                spell.Type = "DamageShield";
                spell.Uninterruptible = true;
                spell.DamageType = (int) EDamageType.Matter; //Spirit DMG Type
                m_DamageShield = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DamageShield);
            }
            return m_DamageShield;
        }
    }
    private Spell m_OEMEffect;
    private Spell OEMEffect
    {
        get
        {
            if (m_OEMEffect == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 5;
                spell.Duration = 5;
                spell.ClientEffect = 4858;
                spell.Icon = 4858;
                spell.Value = 1;
                spell.Name = "Mechanism Effect";
                spell.TooltipId = 5126;
                spell.SpellID = 11864;
                spell.Target = "Self";
                spell.Type = ESpellType.PowerRegenBuff.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_OEMEffect = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OEMEffect);
            }
            return m_OEMEffect;
        }
    }
    #endregion
}
#endregion Organic Energy Mechanism

#region Organic Energy Mechanism adds
public class EnergyMechanismAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public EnergyMechanismAddBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1800;
    }

    public override void Think()
    {
        Body.IsWorthReward = false; //worth no reward
        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            if (Util.Chance(15) && Body.TargetObject != null)
            {
                if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.StrConDebuff))
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastSCDebuff), 3000);
                }
            }
            if (Util.Chance(15) && Body.TargetObject != null)
            {
                if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.MeleeHasteDebuff))
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastHasteDebuff), 3000);
                }
            }
            if (Util.Chance(15) && Body.TargetObject != null)
            {                    
                if(!target.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity))
                {
                    Body.CastSpell(FeederRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
        }

        base.Think();
    }

    public int CastSCDebuff(EcsGameTimer timer)
    {
        if (Body.TargetObject != null)
        {
            Body.CastSpell(FeederSCDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        return 0;
    }
    public int CastHasteDebuff(EcsGameTimer timer)
    {
        if (Body.TargetObject != null)
        {
            Body.CastSpell(FeederHasteDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        return 0;
    }
    private Spell m_FeederSCDebuff;
    private Spell FeederSCDebuff
    {
        get
        {
            if (m_FeederSCDebuff == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 35;
                spell.ClientEffect = 5408;
                spell.Icon = 5408;
                spell.Name = "S/C Debuff";
                spell.TooltipId = 5408;
                spell.Range = 1200;
                spell.Value = 85;
                spell.Duration = 60;
                spell.SpellID = 11713;
                spell.Target = "Enemy";
                spell.Type = "StrengthConstitutionDebuff";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Energy;
                m_FeederSCDebuff = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FeederSCDebuff);
            }
            return m_FeederSCDebuff;
        }
    }

    private Spell m_FeederHasteDebuff;
    private Spell FeederHasteDebuff
    {
        get
        {
            if (m_FeederHasteDebuff == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 35;
                spell.ClientEffect = 5427;
                spell.Icon = 5427;
                spell.Name = "Haste Debuff";
                spell.TooltipId = 5427;
                spell.Range = 1200;
                spell.Value = 24;
                spell.Duration = 60;
                spell.SpellID = 11715;
                spell.Target = "Enemy";
                spell.Type = "CombatSpeedDebuff";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Energy;
                m_FeederHasteDebuff = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FeederHasteDebuff);
            }
            return m_FeederHasteDebuff;
        }
    }
    private Spell m_FeederRoot;
    private Spell FeederRoot
    {
        get
        {
            if (m_FeederRoot == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 11027;
                spell.Icon = 5440;
                spell.Name = "Root";
                spell.Description = "Target moves 40% slower for the spell's duration.";
                spell.TooltipId = 5440;
                spell.Range = 1200;
                spell.Value = 60;
                spell.Duration = 60;
                spell.SpellID = 11865;
                spell.Target = "Enemy";
                spell.Type = ESpellType.SpeedDecrease.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Body;
                m_FeederRoot = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FeederRoot);
            }
            return m_FeederRoot;
        }
    }
}
#endregion Organic Energy Mechanism adds