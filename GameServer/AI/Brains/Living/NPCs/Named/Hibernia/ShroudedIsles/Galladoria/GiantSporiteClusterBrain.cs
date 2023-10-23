using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

#region Giant Sporite Cluster
public class GiantSporiteClusterBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public GiantSporiteClusterBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
        {
            if (Util.Chance(5) && Body.TargetObject != null)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastAOEDD), 3000);
            }
            foreach (GameNpc copy in Body.GetNPCsInRadius(5000))
            {
                if (copy != null)
                {
                    if (copy.IsAlive && copy.Brain is SporiteClusterAddsBrain brain)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!brain.HasAggro)
                            brain.AddToAggroList(target, 10);
                    }
                }
            }
        }
        base.Think();
    }
    public int CastAOEDD(EcsGameTimer timer)
    {
        Body.CastSpell(GSCAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        return 0;
    }

    private Spell m_GSCAoe;
    private Spell GSCAoe
    {
        get
        {
            if (m_GSCAoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 25;
                spell.ClientEffect = 4568;
                spell.Icon = 4568;
                spell.Damage = 200;
                spell.Name = "Xaga Staff Bomb";
                spell.TooltipId = 4568;
                spell.Radius = 200;
                spell.Range = 600;
                spell.SpellID = 11709;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Cold;
                m_GSCAoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GSCAoe);
            }

            return m_GSCAoe;
        }
    }
}
#endregion Giant Sporite Cluster

#region Giant Sporite Cluster adds
public class SporiteClusterAddsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SporiteClusterAddsBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
    }
    public override void Think()
    {
        if(!CheckProximityAggro())
        {
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if(HasAggro && Body.TargetObject != null)
        {
            foreach(GameNpc copy in Body.GetNPCsInRadius(5000))
            {
                if(copy != null)
                {
                    if(copy.IsAlive && Body != copy && copy.Brain is SporiteClusterAddsBrain brain)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!brain.HasAggro)
                            brain.AddToAggroList(target, 10);
                    }
                }
            }
            foreach (GameNpc boss in Body.GetNPCsInRadius(5000))
            {
                if (boss != null)
                {
                    if (boss.IsAlive && boss.Brain is GiantSporiteClusterBrain brain1)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!brain1.HasAggro)
                            brain1.AddToAggroList(target, 10);
                    }
                }
            }
        }
        base.Think();
    }
}
#endregion Giant Sporite Cluster adds