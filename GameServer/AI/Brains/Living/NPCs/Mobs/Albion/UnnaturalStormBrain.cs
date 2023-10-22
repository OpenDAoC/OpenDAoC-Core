using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class UnnaturalStormBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public UnnaturalStormBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 2500;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if(HasAggro && Body.TargetObject != null)
        {
            if (!Body.IsCasting)
                Body.CastSpell(StormDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
        }
        base.Think();
    }
    #region Spell
    private Spell m_StormDD;
    private Spell StormDD
    {
        get
        {
            if (m_StormDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 3;
                spell.Power = 0;
                spell.ClientEffect = 3508;
                spell.Icon = 3508;
                spell.Damage = 200;
                spell.DamageType = (int)EDamageType.Energy;
                spell.Name = "Storm Lightning";
                spell.Range = 2500;
                spell.SpellID = 11947;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_StormDD = new Spell(spell, 50);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_StormDD);
            }
            return m_StormDD;
        }
    }
    #endregion
}

public class UnnaturalStormAddsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public UnnaturalStormAddsBrain() : base()
    {
        AggroLevel = 0;
        AggroRange = 0;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}

public class UnnaturalStormControllerBrain : APlayerVicinityBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public UnnaturalStormControllerBrain()
        : base()
    {
        ThinkInterval = 1000;
    }
    public override void Think()
    {
        uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
        uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
        //log.Warn("Current time: " + hour + ":" + minute);
        foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
        {
            if (npc != null && npc.IsAlive && npc.Brain is UnnaturalStormBrain brain)
            {
                if (!brain.HasAggro && hour >= 7 && hour < 18)
                {
                    npc.RemoveFromWorld();

                    foreach (GameNpc adds in Body.GetNPCsInRadius(8000))
                    {
                        if (adds != null && adds.IsAlive && adds.Brain is UnnaturalStormAddsBrain)
                            adds.RemoveFromWorld();
                    }
                }
            }
        }
        if (hour == 18 && minute == 30)
            SpawnUnnaturalStorm();
		
    }

    public override void KillFSM()
    {
    }

    public void SpawnUnnaturalStorm()
    {
        foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
        {
            if (npc.Brain is UnnaturalStormBrain)
                return;
        }
        UnnaturalStorm boss = new UnnaturalStorm();
        boss.X = Body.X;
        boss.Y = Body.Y;
        boss.Z = Body.Z;
        boss.Heading = Body.Heading;
        boss.CurrentRegion = Body.CurrentRegion;
        boss.AddToWorld();
    }
}