using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.AI.Brain;

#region Oona
public class OonaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public OonaBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 1000;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if (Body.IsAlive)
		{
			if (!Body.Spells.Contains(OonaDD))
				Body.Spells.Add(OonaDD);
			if (!Body.Spells.Contains(OonaBolt))
				Body.Spells.Add(OonaBolt);
		}
		if (!CheckProximityAggro())
		{
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		if (Body.TargetObject != null && HasAggro)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive && npc.Brain is OonaUndeadSoldierBrain brain)
				{
					if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
						brain.AddToAggroList(target, 100);
				}
			}
			foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive && npc.Brain is OonaUndeadAddBrain brain)
				{
					if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
						brain.AddToAggroList(target, 100);
				}
			}
			if (!Body.IsCasting && !Body.IsMoving)
			{
				foreach (Spell spells in Body.Spells)
				{
					if (spells != null)
					{
						if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
							Body.StopFollowing();
						else
							Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

						Body.TurnTo(Body.TargetObject);
						if (Util.Chance(100))
						{
							if (Body.GetSkillDisabledDuration(OonaBolt) == 0)
								Body.CastSpell(OonaBolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							else
								Body.CastSpell(OonaDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
						}
					}
				}
			}
		}
		base.Think();
	}

	#region Spells
	private Spell m_OonaDD;
	private Spell OonaDD
	{
		get
		{
			if (m_OonaDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.Power = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4111;
				spell.Icon = 4111;
				spell.Damage = 330;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Aurora Blast";
				spell.Range = 1650;
				spell.SpellID = 12004;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();					
				m_OonaDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OonaDD);
			}
			return m_OonaDD;
		}
	}
	
	private Spell m_OonaBolt;
	private Spell OonaBolt
	{
		get
		{
			if (m_OonaBolt == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = Util.Random(15, 20);
				spell.ClientEffect = 4559;
				spell.Icon = 4559;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Bolt of Uncreation";
				spell.Range = 1800;
				spell.SpellID = 12005;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.Type = ESpellType.Bolt.ToString();
				m_OonaBolt = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OonaBolt);
			}
			return m_OonaBolt;
		}
	}
	#endregion
}
#endregion Oona

#region Oona's Undead Soldiers
public class OonaUndeadSoldierBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public OonaUndeadSoldierBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Oona's Undead Soldiers

#region Oona's Undead adds
public class OonaUndeadAddBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public OonaUndeadAddBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Oona's Undead adds