using System;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

public class ConservatorBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ConservatorBrain()
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
    protected virtual int PoisonTimer(EcsGameTimer timer)
    {
        if (Body.TargetObject != null)
        {
            Body.CastSpell(COPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            spampoison = false;
        }
        return 0;
    }
    protected virtual int AoeTimer(EcsGameTimer timer)//1st timer to spam broadcast before real spell
    {
        if (Body.TargetObject != null)
        {
            BroadcastMessage(String.Format(Body.Name + " gathers energy from the water..."));
            if (spamaoe == true)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RealAoe), 5000);//5s
            }
        }
        return 0;
    }
    protected virtual int RealAoe(EcsGameTimer timer)//real timer to cast spell and reset check
    {
        if (Body.TargetObject != null)
        {
            Body.CastSpell(COaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            spamaoe = false;
        }
        return 0;
    }
    public static bool spampoison = false;
    public static bool spamaoe = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            spamaoe = false;
            spampoison = false;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
            Body.MaxSpeedBase = npcTemplate.MaxSpeed;
        }          
        if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
            spamaoe = false;
            spampoison = false;
        }
        if(Body.IsOutOfTetherRange && Body.TargetObject != null)
        {
            Body.StopFollowing();
            Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
            GameLiving target = Body.TargetObject as GameLiving;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
            if (target != null)
            {
                if (!target.IsWithinRadius(spawn, 800))
                {
                    Body.MaxSpeedBase = 0;
                }
                else
                    Body.MaxSpeedBase = npcTemplate.MaxSpeed;
            }
        }
        if (HasAggro && Body.InCombat)
        {
            if (Body.TargetObject != null)
            {
                if (spampoison == false)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
                    {
                        Body.TurnTo(Body.TargetObject);
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PoisonTimer), 5000);
                        spampoison = true;
                    }
                }
                if (spamaoe == false)
                {
                    Body.TurnTo(Body.TargetObject);
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(AoeTimer), Util.Random(15000, 20000));//15s to avoid being it too often called
                    spamaoe = true;
                }
            }
        }
        base.Think();
    }

    public Spell m_co_poison;
    public Spell COPoison
    {
        get
        {
            if (m_co_poison == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 40;
                spell.ClientEffect = 4445;
                spell.Icon = 4445;
                spell.Damage = 45;
                spell.Name = "Essense of World Soul";
                spell.Description = "Inflicts powerfull magic damage to the target, then target dies in painfull agony";
                spell.Message1 = "You are wracked with pain!";
                spell.Message2 = "{0} is wracked with pain!";
                spell.Message3 = "You look healthy again.";
                spell.Message4 = "{0} looks healthy again.";
                spell.TooltipId = 4445;
                spell.Range = 1800;
                spell.Duration = 40;
                spell.Frequency = 10; 
                spell.SpellID = 11703;
                spell.Target = "Enemy";
                spell.Type = "DamageOverTime";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
                m_co_poison = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_co_poison);
            }
            return m_co_poison;
        }
    }

    public Spell m_co_aoe;
    public Spell COaoe
    {
        get
        {
            if (m_co_aoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.ClientEffect = 3510;
                spell.Icon = 3510;
                spell.TooltipId = 3510;
                spell.Damage = 550;
                spell.Range = 1800;
                spell.Radius = 1200;
                spell.SpellID = 11704;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_co_aoe = new Spell(spell, 70);                   
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_co_aoe);
            }
            return m_co_aoe;
        }
    }
}