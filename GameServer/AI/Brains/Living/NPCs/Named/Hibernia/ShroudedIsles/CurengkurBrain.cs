using Core.Database.Tables;

namespace Core.GS.AI.Brains;

#region Curengkur
public class CurengkurBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public CurengkurBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
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
		Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
		if(Body.IsWithinRadius(spawn, 800))
        {
			Body.Health += 200;
        }
		if (HasAggro && Body.TargetObject != null)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is CurengkurNestBrain brain)
				{
					if (!brain.HasAggro && target.IsAlive && target != null)
						brain.AddToAggroList(target, 10);
				}
			}				
			if (Util.Chance(50) && !Body.IsCasting)
				Body.CastSpell(CurengkurDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			if (Util.Chance(50) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
				Body.CastSpell(CurengkurPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
		}
		base.Think();
	}
	#region Spells
	private Spell m_CurengkurDD;
	public Spell CurengkurDD
	{
		get
		{
			if (m_CurengkurDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 10;
				spell.Power = 0;
				spell.ClientEffect = 4159;
				spell.Icon = 4159;
				spell.Damage = 400;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Curengkur's Strike";
				spell.Range = 1500;
				spell.SpellID = 11903;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_CurengkurDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CurengkurDD);
			}
			return m_CurengkurDD;
		}
	}		
	private Spell m_CurengkurPoison;
	private Spell CurengkurPoison
	{
		get
		{
			if (m_CurengkurPoison == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 3475;
				spell.Icon = 3475;
				spell.TooltipId = 3475;
				spell.Name = "Poison";
				spell.Description = "Inflicts 100 damage to the target every 3 sec for 30 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.Damage = 100;
				spell.Duration = 30;
				spell.Frequency = 30;
				spell.Range = 500;
				spell.SpellID = 11904;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.DamageType = (int)EDamageType.Body;
				spell.Uninterruptible = true;
				m_CurengkurPoison = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CurengkurPoison);
			}
			return m_CurengkurPoison;
		}
	}
	#endregion
}
#endregion Curengkur

#region Curengkur Nest
public class CurengkurNestBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public CurengkurNestBrain() : base()
	{
		AggroLevel = 0;
		AggroRange = 800;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if (HasAggro)
			Body.CastSpell(CurengkurDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);

		base.Think();
	}

	private Spell m_CurengkurDD2;
	public Spell CurengkurDD2
	{
		get
		{
			if (m_CurengkurDD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 4;
				spell.Power = 0;
				spell.ClientEffect = 1141;
				spell.Icon = 1141;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Curengkur's Radiation";
				spell.Range = 0;
				spell.Radius = 800;
				spell.SpellID = 11903;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_CurengkurDD2 = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CurengkurDD2);
			}
			return m_CurengkurDD2;
		}
	}
}
#endregion Curengkur Nest