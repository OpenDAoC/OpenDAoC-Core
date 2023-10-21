using System;
using Core.Database.Tables;

namespace Core.GS.AI.Brains;

public class NogoribandoBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public NogoribandoBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled = false;
	public static bool IsBig = false;
	public static bool IsSmall = false;
	public static bool IsChangingSize = false;
	public static bool IsInCombat = false;
	public int ChangeSizeToBig(EcsGameTimer timer)
	{
		if (HasAggro && Body.IsAlive)
		{
			IsBig = true;
			IsSmall = false;
			Body.Size = 200;
			Body.Strength = 400;
			Body.Quickness = 50;
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ChangeSizeToSmall), 30000);
		}
		return 0;
	}
	public int ChangeSizeToSmall(EcsGameTimer timer)
    {
		if (HasAggro && Body.IsAlive)
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164519);
			IsSmall = true;
			IsBig = false;
			Body.CastSpell(Boss_Haste_Buff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			Body.Size = 100;
			Body.Strength = npcTemplate.Strength;
			Body.Quickness = npcTemplate.Quickness;
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ChangeSizeToBig), 30000);
		}
		return 0;
    }
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled = false;
			IsChangingSize = false;
			IsSmall = false;
			IsBig = false;
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164519);
			if (IsInCombat == false)
			{
				Body.Size = Convert.ToByte(npcTemplate.Size);
				Body.Strength = npcTemplate.Strength;
				Body.Quickness = npcTemplate.Quickness;
				IsInCombat = true;
			}
		}
		if (Body.InCombat && Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			if (IsPulled == false)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "NogoribandoBaf")
							AddAggroListTo(npc.Brain as StandardMobBrain);
					}
				}
				IsInCombat = false;
				IsPulled = true;
			}
			GameLiving target = Body.TargetObject as GameLiving;
			if(target != null)
            {
				if(!target.effectListComponent.ContainsEffectForEffectType(EEffect.StrConDebuff))
					Body.CastSpell(Boss_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
			if(IsChangingSize==false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ChangeSizeToBig), 5000);
				IsChangingSize = true;
            }
			if(IsSmall)
				Body.CastSpell(Boss_Haste_Buff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.Think();
	}
	private Spell m_Boss_SC_Debuff;
	private Spell Boss_SC_Debuff
	{
		get
		{
			if (m_Boss_SC_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.Duration = 60;
				spell.Value = 75;
				spell.ClientEffect = 4387;
				spell.Icon = 4387;
				spell.TooltipId = 4387;
				spell.Name = "Vitality Dispersal";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11837;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.StrengthConstitutionDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Boss_SC_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_SC_Debuff);
			}
			return m_Boss_SC_Debuff;
		}
	}
	private Spell m_Boss_Haste_Buff;
	private Spell Boss_Haste_Buff
	{
		get
		{
			if (m_Boss_Haste_Buff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 35;
				spell.Duration = 28;
				spell.ClientEffect = 10535;
				spell.Icon = 10535;
				spell.Name = "Nogoribando's Haste";
				spell.Message2 = "{0} begins attacking faster!";
				spell.Message4 = "{0}'s attacks return to normal.";
				spell.TooltipId = 10535;
				spell.Range = 0;
				spell.Value = 50;
				spell.SpellID = 11838;
				spell.Target = "Self";
				spell.Type = ESpellType.CombatSpeedBuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Boss_Haste_Buff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_Haste_Buff);
			}
			return m_Boss_Haste_Buff;
		}
	}
}