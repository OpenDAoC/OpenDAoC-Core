using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

#region Colialt
public class ColialtBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public ColialtBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool ColialtPhase = false;
	private bool CanFollow = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			ColialtPhase = false;
			CanFollow = false;
		}
		if (HasAggro && Body.TargetObject != null)
		{
			foreach(GameNpc npc in Body.GetNPCsInRadius(3000))
            {
				if(npc != null && npc.IsAlive && npc.Brain is ColialtAddsBrain brain)
                {
					GameLiving target = Body.TargetObject as GameLiving;
					if (!brain.HasAggro && target != null)
						brain.AddToAggroList(target, 10);
                }
            }
			if (Body.HealthPercent <= 30)
			{
				ColialtPhase = true;
				if (Body.attackComponent.AttackState && ColialtPhase)
					Body.attackComponent.StopAttack();

				if (Body.TargetObject != null)
				{
					if (!Body.IsCasting && !Body.IsMoving && ColialtPhase)
					{
						CanFollow = false;
						if (ColialtLifeDrain != null)
						{
							if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, ColialtLifeDrain.Range))
								Body.StopFollowing();
							else
								Body.Follow(Body.TargetObject, ColialtLifeDrain.Range - 50, 5000);

							Body.TurnTo(Body.TargetObject);
							Body.CastSpell(ColialtLifeDrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
						}
					}
				}
			}
			else
			{
				ColialtPhase = false;
				if (CanFollow == false)
				{
					Body.StopFollowing();//remove follow target/loc so he can follow actually again
					CanFollow = true;
				}
				Body.StopCurrentSpellcast();
				if(Body.TargetObject != null)
					Body.Follow(Body.TargetObject, 50, 3000);
			}
		}
		base.Think();
	}
	private Spell m_ColialtLifeDrain;
	private Spell ColialtLifeDrain
	{
		get
		{
			if (m_ColialtLifeDrain == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = 0;
				spell.ClientEffect = 14352;
				spell.Icon = 14352;
				spell.TooltipId = 14352;
				spell.Damage = 700;
				spell.Name = "Lifedrain";
				spell.Range = 1800;
				spell.SpellID = 11897;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Body;
				m_ColialtLifeDrain = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ColialtLifeDrain);
			}
			return m_ColialtLifeDrain;
		}
	}
}
#endregion Colialt

#region Colialt adds
public class ColialtAddsBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public ColialtAddsBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Colialt adds