using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

public class SpriggitBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SpriggitBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1500;
	}
	private bool mobHasAggro = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	public override void Think()
	{
		if(!CheckProximityAggro())
        {
			mobHasAggro = false;
        }
		if (HasAggro && Body.TargetObject != null)
		{
			if(!mobHasAggro)
            {
				BroadcastMessage(String.Format("Spriggit crackles as he attacks {0}!",Body.TargetObject.Name));
				mobHasAggro = true;
            }
			GameLiving target = Body.TargetObject as GameLiving;
			if(target != null && target.IsAlive)
            {
				if(!target.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity) && Util.Chance(25))
					Body.CastSpell(SpriggitRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if(Util.Chance(30))
					Body.CastSpell(SpriggitDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive && npc.PackageID == "SpriggitBaf")
					AddAggroListTo(npc.Brain as StandardMobBrain);
			}
		}
		base.Think();
	}
	#region Spells
	private Spell m_SpriggitDD;
	private Spell SpriggitDD
	{
		get
		{
			if (m_SpriggitDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.Power = 0;
				spell.RecastDelay = Util.Random(10,15);
				spell.ClientEffect = 161;
				spell.Icon = 161;
				spell.Damage = 90;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Frost Blast";
				spell.Range = 1500;
				spell.SpellID = 11941;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				m_SpriggitDD = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpriggitDD);
			}
			return m_SpriggitDD;
		}
	}
	private Spell m_SpriggitRoot;
	private Spell SpriggitRoot
	{
		get
		{
			if (m_SpriggitRoot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.Power = 0;
				spell.RecastDelay = Util.Random(25, 35);
				spell.ClientEffect = 5204;
				spell.Icon = 5204;
				spell.TooltipId = 5204;
				spell.Duration = 30;
				spell.Value = 99;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Root";
				spell.Range = 1500;
				spell.SpellID = 11942;
				spell.Target = "Enemy";
				spell.Type = ESpellType.SpeedDecrease.ToString();
				spell.Uninterruptible = true;
				m_SpriggitRoot = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpriggitRoot);
			}
			return m_SpriggitRoot;
		}
	}
	#endregion
}