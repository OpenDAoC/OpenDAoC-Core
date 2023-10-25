using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

public class QueenCliodnaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public QueenCliodnaBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}

	public override void Think()
	{
		if (Body.IsAlive)
		{
			if (!Body.Spells.Contains(Cliodna_stun))
				Body.Spells.Add(Cliodna_stun);
			if (!Body.Spells.Contains(CliodnaDD))
				Body.Spells.Add(CliodnaDD);
		}
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		if (HasAggro && Body.TargetObject != null)
		{
			foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive && npc.PackageID == "CliodnaBaf")
					AddAggroListTo(npc.Brain as StandardMobBrain);
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
							GameLiving target = Body.TargetObject as GameLiving;
							if (target != null)
							{
								if (!Body.IsCasting  && !target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
									Body.CastSpell(Cliodna_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								else
									Body.CastSpell(CliodnaDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							}
						}
					}
				}
			}
		}
		base.Think();
	}
	#region Spells
	private Spell m_CliodnaDD;
	private Spell CliodnaDD
	{
		get
		{
			if (m_CliodnaDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4159;
				spell.Icon = 4159;
				spell.Damage = 400;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Dark Blast";
				spell.Range = 1500;
				spell.SpellID = 11892;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_CliodnaDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CliodnaDD);
			}
			return m_CliodnaDD;
		}
	}
	private Spell m_Cliodna_stun;
	private Spell Cliodna_stun
	{
		get
		{
			if (m_Cliodna_stun == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4125;
				spell.Icon = 4125;
				spell.TooltipId = 4125;
				spell.Duration = 9;
				spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
				spell.Name = "Stun";
				spell.Range = 1500;
				spell.SpellID = 11893;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Stun.ToString();
				spell.DamageType = (int)EDamageType.Energy;
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Cliodna_stun = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Cliodna_stun);
			}
			return m_Cliodna_stun;
		}
	}
	#endregion
}