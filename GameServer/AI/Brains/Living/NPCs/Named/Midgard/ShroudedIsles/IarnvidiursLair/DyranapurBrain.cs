using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

public class DyranapurBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public DyranapurBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled = false;
		}
		if (Body.InCombat && HasAggro && Body.TargetObject != null)
		{
			if (IsPulled == false)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "DyranapurBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain);
						}
					}
				}
				IsPulled = true;
			}
			if (Body.TargetObject != null)
			{
				Body.CastSpell(Boss_Lifedrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
		}
		base.Think();
	}


	private Spell m_Boss_Lifedrain;
	private Spell Boss_Lifedrain
	{
		get
		{
			if (m_Boss_Lifedrain == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = Util.Random(10,15);
				spell.ClientEffect = 2610;
				spell.Icon = 2610;
				spell.Name = "Drain Life";
				spell.TooltipId = 2610;
				spell.Damage = 250;
				spell.Range = 1500;
				spell.Value = -30;
				spell.LifeDrainReturn = 30;
				spell.SpellID = 11819;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Lifedrain.ToString();
				spell.DamageType = (int)EDamageType.Body;
				spell.Uninterruptible = true;
				m_Boss_Lifedrain = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_Lifedrain);
			}
			return m_Boss_Lifedrain;
		}
	}
}