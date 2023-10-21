using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

#region Xaga
public class XagaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public XagaBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            if (!RemoveAdds)
            {
                foreach (GameNpc mob_c in Body.GetNPCsInRadius(4000))
                {
                    if (mob_c != null)
                    {
                        if (mob_c?.Brain is BeathaBrain brain1 && mob_c.IsAlive && brain1.HasAggro)
                            brain1.ClearAggroList();
                        if (mob_c?.Brain is TineBrain brain2 && mob_c.IsAlive && brain2.HasAggro)
                            brain2.ClearAggroList();
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
            RemoveAdds = false;
        base.Think();
    }
    
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (Body.IsAlive)
        {
            foreach (GameNpc mob_c in Body.GetNPCsInRadius(4000))
            {
                if (mob_c != null)
                {
                    if (mob_c?.Brain is BeathaBrain brain1 && mob_c.IsAlive && !brain1.HasAggro)
                        AddAggroListTo(brain1);
                    if (mob_c?.Brain is TineBrain brain2 && mob_c.IsAlive && !brain2.HasAggro)
                        AddAggroListTo(brain2);
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
}
#endregion Xaga

#region Beatha
public class BeathaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BeathaBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }       
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (Body.IsAlive)
        {
            foreach (GameNpc mob_c in Body.GetNPCsInRadius(4000))
            {
                if (mob_c != null)
                {
                    if (mob_c?.Brain is XagaBrain brain1 && mob_c.IsAlive && mob_c.IsAvailable && !brain1.HasAggro)
                        AddAggroListTo(brain1);
                    if (mob_c?.Brain is TineBrain brain2 && mob_c.IsAlive && mob_c.IsAvailable && !brain2.HasAggro)
                        AddAggroListTo(brain2);
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public static bool path1 = false;
    public static bool path2 = false;
    public static bool path3 = false;
    public static bool path4 = false;
    public override void Think()
    {
        if(Body.IsAlive)
        {
            Point3D point1 = new Point3D(27572,54473,13213);
            Point3D point2 = new Point3D(27183, 54530, 13213);
            Point3D point3 = new Point3D(27213, 55106, 13213);
            Point3D point4 = new Point3D(27581, 55079, 13213);
            if (!Body.IsWithinRadius(point1, 20) && path1 == false)
            {
                Body.WalkTo(point1, 250);
            }
            else
            {
                path1 = true;
                path4 = false;
                if (!Body.IsWithinRadius(point2, 20) && path1 == true && path2 == false)
                {
                    Body.WalkTo(point2, 250);
                }
                else
                {
                    path2 = true;
                    if (!Body.IsWithinRadius(point3, 20) && path1 == true && path2 == true && path3 == false)
                    {
                        Body.WalkTo(point3, 250);
                    }
                    else
                    {
                        path3 = true;
                        if (!Body.IsWithinRadius(point4, 20) && path1 == true && path2 == true && path3 == true && path4 == false)
                        {
                            Body.WalkTo(point4, 250);
                        }
                        else
                        {
                            path4 = true;
                            path1 = false;
                            path2 = false;
                            path3 = false;
                        }
                    }
                }
            }
        }
        if(!CheckProximityAggro())
        {
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.IsAlive)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            if (target != null)
            {
                Body.SetGroundTarget(target.X, target.Y, target.Z);
                Body.CastSpell(BeathaAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
            }
        }
        base.Think();
    }
    private Spell m_BeathaAoe;
    private Spell BeathaAoe
    {
        get
        {
            if (m_BeathaAoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = Util.Random(4,8);
                spell.ClientEffect = 4568;
                spell.Icon = 4568;
                spell.Damage = 450;
                spell.Name = "Beatha's Void";
                spell.TooltipId = 4568;
                spell.Range = 3000;
                spell.Radius = 450;
                spell.SpellID = 11707;
                spell.Target = "Area";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Cold;
                m_BeathaAoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BeathaAoe);
            }
            return m_BeathaAoe;
        }
    }
}
#endregion Beatha

#region Tine
public class TineBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public TineBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }

    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (Body.IsAlive)
        {
            foreach (GameNpc mob_c in Body.GetNPCsInRadius(4000))
            {
                if (mob_c != null)
                {
                    if (mob_c?.Brain is XagaBrain brain1 && mob_c.IsAlive && mob_c.IsAvailable && !brain1.HasAggro)
                        AddAggroListTo(brain1);
                    if (mob_c?.Brain is BeathaBrain brain2 && mob_c.IsAlive && mob_c.IsAvailable && !brain2.HasAggro)
                        AddAggroListTo(brain2);
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public static bool path1_2 = false;
    public static bool path2_2 = false;
    public static bool path3_2 = false;
    public static bool path4_2 = false;
    public override void Think()
    {
        if (Body.IsAlive)
        {
            Point3D point1 = new Point3D(27168, 54598, 13213);
            Point3D point2 = new Point3D(27597, 54579, 13213);
            Point3D point3 = new Point3D(27606, 55086, 13213);
            Point3D point4 = new Point3D(27208, 55133, 13213);
            if (!Body.IsWithinRadius(point1, 20) && path1_2 == false)
            {
                Body.WalkTo(point1, 250);
            }
            else
            {
                path1_2 = true;
                path4_2 = false;
                if (!Body.IsWithinRadius(point2, 20) && path1_2 == true && path2_2 == false)
                {
                    Body.WalkTo(point2, 250);
                }
                else
                {
                    path2_2 = true;
                    if (!Body.IsWithinRadius(point3, 20) && path1_2 == true && path2_2 == true && path3_2 == false)
                    {
                        Body.WalkTo(point3, 250);
                    }
                    else
                    {
                        path3_2 = true;
                        if (!Body.IsWithinRadius(point4, 20) && path1_2 == true && path2_2 == true && path3_2 == true && path4_2 == false)
                        {
                            Body.WalkTo(point4, 250);
                        }
                        else
                        {
                            path4_2 = true;
                            path1_2 = false;
                            path2_2 = false;
                            path3_2 = false;
                        }
                    }
                }
            }
        }
        if (!CheckProximityAggro())
        {
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.IsAlive)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            if (target != null)
            {
                Body.SetGroundTarget(target.X, target.Y, target.Z);
                Body.CastSpell(TineAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
            }
        }
        base.Think();
    }
    private Spell m_TineAoe;
    private Spell TineAoe
    {
        get
        {
            if (m_TineAoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = Util.Random(4,8);
                spell.ClientEffect = 4227;
                spell.Icon = 4227;
                spell.Damage = 450;
                spell.Name = "Tine's Fire";
                spell.TooltipId = 4227;
                spell.Range = 3000;
                spell.Radius = 450;
                spell.SpellID = 11708;
                spell.Target = "Area";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Heat;
                m_TineAoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TineAoe);
            }
            return m_TineAoe;
        }
    }
}
#endregion Tine