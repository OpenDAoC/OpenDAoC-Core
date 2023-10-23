using System;
using System.Collections;
using System.Collections.Generic;
using Core.GS.AI;
using Core.GS.Calculators;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.RealmAbilities;

public class NfRaChargeEffect : StaticEffect, IGameEffect
{
	protected const String delveString = "Grants unbreakable speed 3 for 15 second duration. Grants immunity to roots, stun, snare and mesmerize spells. Target will still take damage from snare/root spells that do damage.";
	GameLiving m_living;
	protected long m_startTick;
	protected EcsGameTimer m_expireTimer;

	public override void Start(GameLiving living)
	{
		m_living = living;
		//Send messages
		if (m_living is GamePlayer)
		{
			((GamePlayer)m_living).Out.SendMessage("You begin to charge wildly!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
		}
		else if (m_living is GameNpc)
		{
			IControlledBrain icb = ((GameNpc)m_living).Brain as IControlledBrain;
			if (icb != null && icb.Body != null)
			{
				GamePlayer playerowner = icb.GetPlayerOwner();

				if (playerowner != null)
				{
					playerowner.Out.SendMessage("The " + icb.Body.Name + " charges its prey!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				}
			}
		}
		else
			return;
		
		m_startTick = living.CurrentRegion.Time;
		foreach (GamePlayer t_player in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			t_player.Out.SendSpellEffectAnimation(living, living, 7035, 0, false, 1);
		}

		//sets player into combat mode
		living.LastAttackTickPvP = m_startTick;
		ArrayList speedSpells = new ArrayList();
		lock(living.EffectList)
		{
			foreach (IGameEffect effect in living.EffectList)
			{
				if (effect is GameSpellEffect == false) continue;
				if ((effect as GameSpellEffect).Spell.SpellType == ESpellType.SpeedEnhancement)
					speedSpells.Add(effect);
			}
		}
		foreach (GameSpellEffect spell in speedSpells)
			spell.Cancel(false);
		m_living.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, this, MaxMovementSpeedCalculator.SPEED3);
		m_living.TempProperties.SetProperty("Charging", true);
		if (m_living is GamePlayer)
			((GamePlayer)m_living).Out.SendUpdateMaxSpeed();
		StartTimers();
		m_living.EffectList.Add(this);
	}

	public override void Cancel(bool playerCancel)
	{
		m_living.TempProperties.RemoveProperty("Charging");
		m_living.EffectList.Remove(this);
		m_living.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, this);
		//Send messages
		if (m_living is GamePlayer)
		{
			GamePlayer player = m_living as GamePlayer;
			player.Out.SendUpdateMaxSpeed();
			player.Out.SendMessage("You no longer seem so crazy!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
		else if (m_living is GameNpc)
		{
			IControlledBrain icb = ((GameNpc)m_living).Brain as IControlledBrain;
			if (icb != null && icb.Body != null)
			{
				GamePlayer playerowner = icb.GetPlayerOwner();

				if (playerowner != null)
				{
					playerowner.Out.SendMessage("The " + icb.Body.Name + " ceases its charge!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				}
			}
		}
		StopTimers();
	}

	protected virtual void StartTimers()
	{
		StopTimers();
		m_expireTimer = new EcsGameTimer(m_living, new EcsGameTimer.EcsTimerCallback(ExpiredCallback), RealmAbilities.NfRaChargeAbility.DURATION * 1000);
	}


	// Stops the timers for this effect
	protected virtual void StopTimers()
	{
		if (m_expireTimer != null)
		{
			m_expireTimer.Stop();
			m_expireTimer = null;
		}
	}


	// The callback method when the effect expires
	protected virtual int ExpiredCallback(EcsGameTimer timer)
	{
		Cancel(false);
		return 0;
	}


	// Name of the effect
	public override string Name { get { return "Charge"; } }


	/// <summary>
	/// Remaining time of the effect in milliseconds
	/// </summary>
	public override Int32 RemainingTime
	{
		get
		{
			EcsGameTimer timer = m_expireTimer;
			if (timer == null || !timer.IsAlive)
				return 0;
			return timer.TimeUntilElapsed;
		}
	}


	// Icon to show on players, can be id
	public override ushort Icon
	{
		get
		{
			if (m_living is GameNpc) return 411;
			else return 3034;
		}
	}

	// Delve Info
	public override IList<string> DelveInfo
	{
		get
		{
			var delveInfoList = new List<string>(4);
			delveInfoList.Add(delveString);

			int seconds = (int)(RemainingTime / 1000);
			if (seconds > 0)
			{
				delveInfoList.Add(" "); //empty line
				if (seconds > 60)
					delveInfoList.Add("- " + seconds / 60 + ":" + (seconds % 60).ToString("00") + " minutes remaining.");
				else
					delveInfoList.Add("- " + seconds + " seconds remaining.");
			}

			return delveInfoList;
		}
	}
}