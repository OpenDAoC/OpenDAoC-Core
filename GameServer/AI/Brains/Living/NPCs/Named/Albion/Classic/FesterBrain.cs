using DOL.Database;
using DOL.GS;

namespace DOL.AI.Brain;

public class FesterBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public FesterBrain() : base()
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
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		if (HasAggro && Body.TargetObject != null)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			if(target != null && !target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
				Body.CastSpell(fester_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.Think();
	}
	#region Spells
	private Spell m_fester_Dot;
	private Spell fester_Dot
	{
		get
		{
			if (m_fester_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = Util.Random(20,30);
				spell.ClientEffect = 3411;
				spell.Icon = 3411;
				spell.Name = "Fester's Venom";
				spell.Description = "Inflicts 80 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 3411;
				spell.Range = 1500;
				spell.Radius = 600;
				spell.Damage = 80;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11985;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_fester_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_fester_Dot);
			}
			return m_fester_Dot;
		}
	}
	#endregion
}