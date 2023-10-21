using System;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

public class ThaneDyggveBrain : StandardMobBrain
{
	protected String[] m_MjollnirAnnounce;
	protected bool castsMjollnir = true;
	private bool CanCastSpell = false;
	public ThaneDyggveBrain() : base()
	{
		CanBAF = false;
		m_MjollnirAnnounce = new String[]
		{
			"You feel your energy draining and {0} summons powerful lightning hammers!",
			"{0} takes another energy drain as he prepares to unleash a raging Mjollnir upon you!"
		};
	}
	public override void Think()
	{
		if(!CheckProximityAggro())
        {
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			CanCastSpell = false;
		}
		if (Body.InCombat && Body.IsAlive && HasAggro)
		{
			if (Body.TargetObject != null)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "ThaneDyggveBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
				if (!CanCastSpell)
				{
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastMjollnir), 2000);
					CanCastSpell = true;
				}
				if (Body.IsCasting)
				{
					if (castsMjollnir)
					{
						int messageNo = Util.Random(1, m_MjollnirAnnounce.Length) - 1;
						BroadcastMessage(String.Format(m_MjollnirAnnounce[messageNo], Body.Name));
					}
					castsMjollnir = false;
				}
				else
					castsMjollnir = true;
			}
		}
		base.Think();
	}		
	/// <summary>
	/// Broadcast relevant messages to the raid.
	/// </summary>
	/// <param name="message">The message to be broadcast.</param>
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}	

	/// <summary>
	/// Cast Mjollnir on the Target
	/// </summary>
	/// <param name="timer">The timer that started this cast.</param>
	/// <returns></returns>
	private int CastMjollnir(EcsGameTimer timer)
	{
		Body.CastSpell(Mjollnir, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetMjollnir), 30000);
		return 0;
	}	
	private int ResetMjollnir(EcsGameTimer timer)
    {				
		CanCastSpell = false;
		return 0;
    }
	#region MjollnirSpell
	private Spell m_Mjollnir;
	/// <summary>
	/// The Mjollnir spell.
	/// </summary>
	protected Spell Mjollnir
	{
		get
		{
			if (m_Mjollnir == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 4;
				spell.Uninterruptible = true;
				spell.RecastDelay = 30;
				spell.ClientEffect = 3541;
				spell.Icon = 3541;
				spell.Description = "Damages the target for 800.";
				spell.Name = "Command Mjollnir";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.Value = 0;
				spell.Duration = 0;
				spell.Damage = 500;
				spell.DamageType = 12;
				spell.SpellID = 3541;
				spell.Target = "Enemy";
				spell.MoveCast = false;
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_Mjollnir = new Spell(spell, 50);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mjollnir);
			}
			return m_Mjollnir;
		}
	}
	#endregion		
}