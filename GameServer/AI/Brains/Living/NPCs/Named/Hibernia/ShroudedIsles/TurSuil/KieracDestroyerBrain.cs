using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

#region Kierac the Destroyer
public class KieracDestroyerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public KieracDestroyerBrain() : base()
    {
        AggroLevel = 100;
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
        if (Body.InCombat && Body.IsAlive && HasAggro)
        {
            if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.Bladeturn))
            {
                Body.CastSpell(Bubble, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        base.Think();
    }
    private Spell m_Bubble;
    private Spell Bubble
    {
        get
        {
            if (m_Bubble == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 10;
                spell.Duration = 10;
                spell.ClientEffect = 5126;
                spell.Icon = 5126;
                spell.TooltipId = 5126;
                spell.Name = "Shield of Pain";
                spell.Range = 0;
                spell.SpellID = 11792;
                spell.Target = "Self";
                spell.Type = ESpellType.Bladeturn.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_Bubble = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bubble);
            }
            return m_Bubble;
        }
    }
}
#endregion Kierac the Destroyer

#region Master of Pain
public class MasterOfPainBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MasterOfPainBrain() : base()
	{
		AggroLevel = 100;
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
		if (Body.InCombat && Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			Body.CastSpell(DebuffSC, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.Think();
	}
	private Spell m_DebuffSC;
	private Spell DebuffSC
	{
		get
		{
			if (m_DebuffSC == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(25,45);
				spell.Duration = 60;
				spell.Value = 75;
				spell.ClientEffect = 4387;
				spell.Icon = 4387;
				spell.TooltipId = 4387;
				spell.Name = "Vitality Dispersal";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11793;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.StrengthConstitutionDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_DebuffSC = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DebuffSC);
			}
			return m_DebuffSC;
		}
	}
}
#endregion Master of Pain