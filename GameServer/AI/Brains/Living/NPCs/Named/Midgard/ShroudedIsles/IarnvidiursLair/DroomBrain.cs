using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class DroomBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public DroomBrain() : base()
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
						if (npc.IsAlive && npc.PackageID == "DroomBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain);
						}
					}
				}
				IsPulled = true;
			}
			GameLiving target = Body.TargetObject as GameLiving;
			if (Util.Chance(15) && Body.TargetObject != null)
			{
				Body.CastSpell(Droom_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (Util.Chance(15) && Body.TargetObject != null)
			{
				Body.CastSpell(Droom_Dot2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (Util.Chance(15) && Body.TargetObject != null)
			{
				if (LivingHasEffect(Body.TargetObject as GameLiving, Droom_SC_Debuff) == false && Body.TargetObject.IsVisibleTo(Body))
				{
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastSCDebuff), 3000);
				}
			}
			if (Util.Chance(15) && Body.TargetObject != null)
			{
				if (LivingHasEffect(Body.TargetObject as GameLiving, Droom_Haste_Debuff) == false && Body.TargetObject.IsVisibleTo(Body))
				{
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastHasteDebuff), 3000);
				}
			}
			if (Util.Chance(15) && Body.TargetObject != null)
			{
				if (LivingHasEffect(Body.TargetObject as GameLiving, DroomDisease) == false && Body.TargetObject.IsVisibleTo(Body))
				{
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDisease), 3000);
				}
			}
		}
		base.Think();
	}
	public int CastSCDebuff(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(Droom_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	public int CastHasteDebuff(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(Droom_Haste_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	public int CastDisease(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(DroomDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	#region Spells
	private Spell m_Droom_Dot;
	private Spell Droom_Dot
	{
		get
		{
			if (m_Droom_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3411;
				spell.Icon = 3411;
				spell.Name = "Droom's Poison";
				spell.Description = "Inflicts 80 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 3411;
				spell.Range = 1500;
				spell.Damage = 80;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11805;
				spell.Target = "Enemy";
				spell.SpellGroup = 1800;
				spell.EffectGroup = 1500;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Droom_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Droom_Dot);
			}
			return m_Droom_Dot;
		}
	}
	private Spell m_Droom_Dot2;
	private Spell Droom_Dot2
	{
		get
		{
			if (m_Droom_Dot2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3475;
				spell.Icon = 4431;
				spell.Name = "Droom's Acid";
				spell.Description = "Inflicts 80 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 4431;
				spell.Range = 1500;
				spell.Damage = 85;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11806;
				spell.Target = "Enemy";
				spell.SpellGroup = 1801;
				spell.EffectGroup = 1501;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Body;
				m_Droom_Dot2 = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Droom_Dot2);
			}
			return m_Droom_Dot2;
		}
	}
	private Spell m_Droom_SC_Debuff;
	private Spell Droom_SC_Debuff
	{
		get
		{
			if (m_Droom_SC_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 30;
				spell.Duration = 60;
				spell.ClientEffect = 2767;
				spell.Icon = 2767;
				spell.Name = "Droom's Debuff S/C";
				spell.TooltipId = 2767;
				spell.Range = 1500;
				spell.Value = 80;
				spell.Radius = 450;
				spell.SpellID = 11807;
				spell.Target = "Enemy";
				spell.Type = ESpellType.StrengthConstitutionDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Droom_SC_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Droom_SC_Debuff);
			}
			return m_Droom_SC_Debuff;
		}
	}
	private Spell m_Droom_Haste_Debuff;
	private Spell Droom_Haste_Debuff
	{
		get
		{
			if (m_Droom_Haste_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 30;
				spell.Duration = 60;
				spell.ClientEffect = 5427;
				spell.Icon = 5427;
				spell.Name = "Droom's Debuff Haste";
				spell.TooltipId = 5427;
				spell.Range = 1500;
				spell.Value = 38;
				spell.Radius = 450;
				spell.SpellID = 11808;
				spell.Target = "Enemy";
				spell.Type = ESpellType.CombatSpeedDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Droom_Haste_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Droom_Haste_Debuff);
			}
			return m_Droom_Haste_Debuff;
		}
	}
	private Spell m_DroomDisease;
	private Spell DroomDisease
	{
		get
		{
			if (m_DroomDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4375;
				spell.Icon = 4375;
				spell.Name = "Black Plague";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.TooltipId = 4375;
				spell.Range = 350;
				spell.Duration = 120;
				spell.SpellID = 11809;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_DroomDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DroomDisease);
			}
			return m_DroomDisease;
		}
	}
	#endregion
}