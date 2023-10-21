using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

#region Dra'argus the Mighty
public class DraargusMightyBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public DraargusMightyBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 300;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (Body.InCombat && Body.IsAlive && HasAggro)
        {
            if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.DamageReturn))
            {
                Body.CastSpell(FireDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        base.Think();
    }
    private Spell m_FireDS;
    private Spell FireDS
    {
        get
        {
            if (m_FireDS == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 60;
                spell.ClientEffect = 57;
                spell.Icon = 57;
                spell.Damage = 120;
                spell.Duration = 60;
                spell.Name = "Dra'argus Shield";
                spell.TooltipId = 57;
                spell.SpellID = 11800;
                spell.Target = "Self";
                spell.Type = "DamageShield";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Heat;
                m_FireDS = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FireDS);
            }
            return m_FireDS;
        }
    }
}
#endregion Dra'argus the Mighty

#region Draugyn Sphere
public class DraugynSphereBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public DraugynSphereBrain() : base()
    {
        AggroLevel = 0;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.IsAlive)
        {
            Body.SetGroundTarget(Body.X, Body.Y, Body.Z);
            Body.CastSpell(Sphere_pbaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

            foreach(GamePlayer player in Body.GetPlayersInRadius(300))
            {
                if(player != null)
                {
                    if(player.IsAlive && AggroTable.ContainsKey(player) && player.Client.Account.PrivLevel == 1)
                    {
                        if(!player.IsWithinRadius(Body,200))
                        {
                            player.MoveTo(Body.CurrentRegionID, Body.X, Body.Y, Body.Z, Body.Heading);
                        }
                    }
                }
            }
        }
        base.Think();
    }
    private Spell m_Sphere_pbaoe;
    private Spell Sphere_pbaoe
    {
        get
        {
            if (m_Sphere_pbaoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 4;
                spell.ClientEffect = 368;
                spell.Icon = 368;
                spell.TooltipId = 368;
                spell.Damage = 300;
                spell.Name = "Sphere Explosion";
                spell.Range = 500;
                spell.Radius = 500;
                spell.SpellID = 11799;
                spell.Target = "Area";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.DamageType = (int)EDamageType.Heat;
                m_Sphere_pbaoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Sphere_pbaoe);
            }
            return m_Sphere_pbaoe;
        }
    }
}
#endregion Draugyn Sphere