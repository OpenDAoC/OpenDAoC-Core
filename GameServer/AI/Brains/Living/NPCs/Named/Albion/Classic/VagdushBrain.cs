using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI.Brains;

public class VagdushBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public VagdushBrain() : base()
	{
		ThinkInterval = 1500;
	}
	private bool CallforHelp = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			CallforHelp = false;
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12742);
			Body.MaxSpeedBase = npcTemplate.MaxSpeed;
		}

		if (HasAggro && Body.TargetObject != null)
		{
			if (!CallforHelp)
			{
				if (Body.HealthPercent <= 10)
				{
					BroadcastMessage("The " + Body.Name + " calls for help!");
					foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
					{
						if (npc != null && npc.IsAlive && npc.PackageID == "VagdushBaf")
							AddAggroListTo(npc.Brain as StandardMobBrain);
					}
					CallforHelp = true;
				}				
			}
			GameLiving target = Body.TargetObject as GameLiving;
			if(!target.IsWithinRadius(Body,Body.AttackRange) && target.IsAlive && target != null)
            {
				Body.MaxSpeedBase = 0;
				if (!Body.IsCasting && Util.Chance(100))
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
						Body.CastSpell(VagdushDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					else
						Body.CastSpell(VagdushDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			else
            {
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12742);
				Body.MaxSpeedBase = npcTemplate.MaxSpeed;
			}
		}
		base.Think();
	}
	#region Spells
	private Spell m_VagdushDisease;
	private Spell VagdushDisease
	{
		get
		{
			if (m_VagdushDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 2;
				spell.RecastDelay = 0;
				spell.ClientEffect = 731;
				spell.Icon = 731;
				spell.TooltipId = 731;
				spell.Name = "Persistent Disease";
				spell.Description = "Inflicts a wasting disease on the target that slows it, weakens it, and inhibits heal spells.";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.Range = 1500;
				spell.Duration = 60;
				spell.SpellID = 11986;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.DamageType = (int)EDamageType.Body; //Energy DMG Type
				m_VagdushDisease = new Spell(spell, 10);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VagdushDisease);
			}
			return m_VagdushDisease;
		}
	}
	private Spell m_VagdushDD;
	private Spell VagdushDD
	{
		get
		{
			if (m_VagdushDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 754;
				spell.Icon = 754;
				spell.Name = "Vagdush Blast";
				spell.Damage = 50;
				spell.Range = 1500;
				spell.SpellID = 11987;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Matter;
				m_VagdushDD = new Spell(spell, 10);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VagdushDD);
			}
			return m_VagdushDD;
		}
	}
	#endregion
}