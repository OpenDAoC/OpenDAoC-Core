using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class QuillanMuireBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public QuillanMuireBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if(HasAggro && Body.TargetObject != null)
        {
			if(!Body.IsCasting && Util.Chance(25))
				Body.CastSpell(QuillanMuire_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (!Body.IsCasting && Util.Chance(25))
				Body.CastSpell(QuillanMuire_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is MuireHerbalistBrain brian)
				{
					if (!brian.HasAggro && brian != null && target != null && target.IsAlive)
						brian.AddToAggroList(target, 10);
				}
			}
			foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
			{
				if (npc != null && npc.IsAlive && npc.PackageID == "QuillanBaf")
					AddAggroListTo(npc.Brain as StandardMobBrain); 
			}
		}
		base.Think();
	}
	#region Spells
	private Spell m_QuillanMuire_DD;
	private Spell QuillanMuire_DD
	{
		get
		{
			if (m_QuillanMuire_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = Util.Random(10, 15);
				spell.ClientEffect = 14353;
				spell.Icon = 14353;
				spell.TooltipId = 14353;
				spell.Damage = 80;
				spell.Name = "Energy Blast";
				spell.Range = 1500;
				spell.SpellID = 11948;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy;
				m_QuillanMuire_DD = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_QuillanMuire_DD);
			}
			return m_QuillanMuire_DD;
		}
	}
	private Spell m_QuillanMuire_DD2;
	private Spell QuillanMuire_DD2
	{
		get
		{
			if (m_QuillanMuire_DD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = Util.Random(8, 12);
				spell.ClientEffect = 4356;
				spell.Icon = 4356;
				spell.TooltipId = 4356;
				spell.Damage = 70;
				spell.Name = "Energy Blast";
				spell.Range = 1500;
				spell.SpellID = 11949;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy;
				m_QuillanMuire_DD2 = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_QuillanMuire_DD2);
			}
			return m_QuillanMuire_DD2;
		}
	}
	#endregion
}

#region Muire herbalist
public class MuireHerbalistBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MuireHerbalistBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1500;
	}
    public override void AttackMostWanted()
    {
		if (Ishealing || IsBuffing || IsBuffingSelf)
			return;
		else
			base.AttackMostWanted();
    }
    private protected bool Ishealing = false;
	private protected bool IsBuffing = false;
	private protected bool IsBuffingSelf = false;
	private protected void HealAndBuff()
    {
		foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
		{
			if (npc.IsAlive && npc != null && npc.Faction == Body.Faction)
			{
				foreach (Spell spell in Body.Spells)
				{
					if (spell != null)
					{
						if (npc.HealthPercent < 50)
						{
							Ishealing = true;
							if (!Body.IsCasting)
							{
								if (Body.TargetObject != npc)
									Body.TargetObject = npc;

								Body.CastSpell(MuireHerbalistHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							}
						}
						if(Body.GetSkillDisabledDuration(MuireHerbalistHeal) > 0)
						{
							Ishealing = false;
							Body.TargetObject = null;
						}
					}
				}
			}
		}
		if (!Ishealing)
		{
			foreach (GameNpc npc in Body.GetNPCsInRadius(500))
			{
				if (npc != null && npc.IsAlive && (npc.Name == "Muire Hero" || npc.Name == "Muire Champion" || npc.Name == "Quillan Muire"))
				{
					if (!Body.IsCasting && !npc.effectListComponent.ContainsEffectForEffectType(EEffect.StrengthBuff))
					{
						IsBuffing = true;
						Body.TargetObject = npc;
						Body.CastSpell(MuireHerbalist_Buff_STR, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
					}
					else
					{
						Body.TargetObject = null;
						IsBuffing = false;
						if (!Body.IsCasting && !Body.effectListComponent.ContainsEffectForEffectType(EEffect.StrengthBuff))
						{
							IsBuffingSelf = true;
							Body.TargetObject = Body;
							Body.CastSpell(MuireHerbalist_Buff_STR, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
						}
						else
						{
							IsBuffingSelf = false;
							Body.TargetObject = null;
						}
					}
				}
			}
		}
	}

    public override void Think()
	{
		if(Body.IsAlive)
        {
			if (!Body.Spells.Contains(MuireHerbalistHeal))
				Body.Spells.Add(MuireHerbalistHeal);

		}
		HealAndBuff();
		base.Think();
    }
    #region Spells
    private Spell m_MuireHerbalistHeal;
	private Spell MuireHerbalistHeal
	{
		get
		{
			if (m_MuireHerbalistHeal == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 3;
				spell.ClientEffect = 1340;
				spell.Icon = 1340;
				spell.TooltipId = 1340;
				spell.Value = 150;
				spell.Name = "Heal";
				spell.Range = 1500;
				spell.SpellID = 11949;
				spell.Target = "Realm";
				spell.Type = ESpellType.Heal.ToString();
				spell.Uninterruptible = true;
				m_MuireHerbalistHeal = new Spell(spell, 15);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MuireHerbalistHeal);
			}
			return m_MuireHerbalistHeal;
		}
	}
	private Spell m_MuireHerbalist_Buff_STR;
	private Spell MuireHerbalist_Buff_STR
	{
		get
		{
			if (m_MuireHerbalist_Buff_STR == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 1451;
				spell.Duration = 1200;
				spell.Icon = 1451;
				spell.TooltipId = 5003;
				spell.Value = 20;
				spell.Name = "Herbalist Strength";
				spell.Range = 1500;
				spell.SpellID = 11950;
				spell.Target = "Realm";
				spell.Type = ESpellType.StrengthBuff.ToString();
				spell.Uninterruptible = true;
				m_MuireHerbalist_Buff_STR = new Spell(spell, 15);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MuireHerbalist_Buff_STR);
			}
			return m_MuireHerbalist_Buff_STR;
		}
	}
	#endregion
}
#endregion Muire herbalist