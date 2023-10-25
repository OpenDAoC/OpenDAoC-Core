using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS;

public class YdeniaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public YdeniaBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 1000;
		ThinkInterval = 1500;
	}
	private bool canPort = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			canPort = false;
			var throwPlayer = Body.TempProperties.GetProperty<EcsGameTimer>("ydenia_teleport");//cancel teleport
			if (throwPlayer != null)
			{
				throwPlayer.Stop();
				Body.TempProperties.RemoveProperty("ydenia_teleport");
			}
		}
		if (Body.TargetObject != null && HasAggro)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive && npc.Name.ToLower() == "dark seithkona" && npc.Brain is StandardMobBrain brain)
				{
					if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
						brain.AddToAggroList(target, 100);
				}
			}
			if(target != null && target.IsAlive)
            {
				if (Util.Chance(100) && !Body.IsCasting)
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.StrConDebuff))
						Body.CastSpell(Ydenia_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if (Util.Chance(100) && !Body.IsCasting)
					Body.CastSpell(YdeniaDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if(Util.Chance(35) && !canPort)
            {
				EcsGameTimer portTimer = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(InitiatePort), Util.Random(25000, 35000));
				Body.TempProperties.SetProperty("ydenia_teleport", portTimer);
				canPort = true;
            }				
		}
		base.Think();
	}
	private int InitiatePort(EcsGameTimer timer)
    {
		GameLiving target = Body.TargetObject as GameLiving;
		BroadcastMessage(String.Format("{0} says, \"Feel the power of the Seithkona, fool!\"", Body.Name));
		YdeniaPort(target);
		return 0;
    }
	private void YdeniaPort(GameLiving target)
    {
		if(target != null && target.IsAlive && target is GamePlayer player)
        {
			switch(Util.Random(1,4))
            {
				case 1: 
					player.MoveTo(100, 664713, 896689, 1553, 2373);
					player.TakeDamage(player, EDamageType.Cold, player.MaxHealth / 7, 0);
					foreach (GamePlayer players in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						if (players != null)
							player.Out.SendSpellEffectAnimation(player, player, 4074, 0, false, 1);
					}
					player.Out.SendMessage("Ydenia of the Seithkona throws you into water and you take damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
					break;
				case 2:
					player.MoveTo(100, 667220, 894261, 1543, 692);
					player.TakeDamage(player, EDamageType.Cold, player.MaxHealth / 7, 0);
					foreach (GamePlayer players in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						if (players != null)
							player.Out.SendSpellEffectAnimation(player, player, 4074, 0, false, 1);
					}
					player.Out.SendMessage("Ydenia of the Seithkona throws you into water and you take damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
					break;
				case 3:
					player.MoveTo(100, 665968, 892792, 1561, 235);
					player.TakeDamage(player, EDamageType.Cold, player.MaxHealth / 7, 0);
					foreach (GamePlayer players in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						if (players != null)
							player.Out.SendSpellEffectAnimation(player, player, 4074, 0, false, 1);
					}
					player.Out.SendMessage("Ydenia of the Seithkona throws you into water and you take damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
					break;
				case 4:
					player.MoveTo(100, 663895, 893446, 1554, 3482);
					player.TakeDamage(player, EDamageType.Cold, player.MaxHealth / 7, 0);
					foreach (GamePlayer players in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						if (players != null)
							player.Out.SendSpellEffectAnimation(player, player, 4074, 0, false, 1);
					}
					player.Out.SendMessage("Ydenia of the Seithkona throws you into water and you take damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
					break;
			}
        }
		canPort = false;
    }
	#region Spells
	private Spell m_YdeniaDD;
	private Spell YdeniaDD
	{
		get
		{
			if (m_YdeniaDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.Power = 0;
				spell.RecastDelay = Util.Random(4,6);
				spell.ClientEffect = 9191;
				spell.Icon = 9191;
				spell.Damage = 320;
				spell.DamageType = (int)EDamageType.Body;
				spell.Name = "Lifedrain";
				spell.Range = 1500;
				spell.SpellID = 12010;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_YdeniaDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_YdeniaDD);
			}
			return m_YdeniaDD;
		}
	}
	private Spell m_Ydenia_SC_Debuff;
	private Spell Ydenia_SC_Debuff
	{
		get
		{
			if (m_Ydenia_SC_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 30;
				spell.Duration = 60;
				spell.ClientEffect = 2767;
				spell.Icon = 2767;
				spell.Name = "Emasculate Strength";
				spell.TooltipId = 2767;
				spell.Range = 1000;
				spell.Value = 66;
				spell.Radius = 400;
				spell.SpellID = 12011;
				spell.Target = "Enemy";
				spell.Type = ESpellType.StrengthConstitutionDebuff.ToString();
				m_Ydenia_SC_Debuff = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Ydenia_SC_Debuff);
			}
			return m_Ydenia_SC_Debuff;
		}
	}
	#endregion
}