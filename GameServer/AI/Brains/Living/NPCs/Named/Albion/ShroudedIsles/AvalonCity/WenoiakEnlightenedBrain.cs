using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class WenoiakEnlightenedBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public WenoiakEnlightenedBrain() : base()
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
		if (Body.InCombat && Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			if (IsPulled == false)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "WenoiakBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with IssordenBaf PackageID
						}
					}
				}
				IsPulled = true;
			}
			if (Util.Chance(25))
				Body.CastSpell(Light_dd, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (Util.Chance(25))
				Body.CastSpell(Light_pbaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.Think();
	}
	private Spell m_Light_dd;
	private Spell Light_dd
	{
		get
		{
			if (m_Light_dd == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = Util.Random(10,15);
				spell.ClientEffect = 1678;
				spell.Icon = 1678;
				spell.TooltipId = 1678;
				spell.Damage = 350;
				spell.Name = "Weno'iak Lighs";
				spell.Range = 1500;
				spell.SpellID = 11797;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Light_dd = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Light_dd);
			}
			return m_Light_dd;
		}
	}

	private Spell m_Light_pbaoe;
	private Spell Light_pbaoe
	{
		get
		{
			if (m_Light_pbaoe == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(20, 25);
				spell.ClientEffect = 1666;
				spell.Icon = 1666;
				spell.TooltipId = 1666;
				spell.Damage = 450;
				spell.Name = "Weno'iak's Annihilate";
				spell.Range = 0;
				spell.Radius = 500;
				spell.SpellID = 11798;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Light_pbaoe = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Light_pbaoe);
			}
			return m_Light_pbaoe;
		}
	}
}