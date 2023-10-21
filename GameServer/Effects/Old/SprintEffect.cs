using Core.GS.ECS;
using Core.GS.RealmAbilities;
using Core.Language;

namespace Core.GS.Effects.Old;

public sealed class SprintEffect : StaticEffect, IGameEffect
{
	/// <summary>
	/// The timer that reduce the endurance every interval
	/// </summary>
	EcsGameTimer m_tickTimer;

	/// <summary>
	/// The amount of timer ticks player was not moving
	/// </summary>
	int m_idleTicks = 0;

	/// <summary>
	/// Start the sprinting on player
	/// </summary>
	public override void Start(GameLiving target)
	{
		base.Start(target);
		if (m_tickTimer != null)
		{
			m_tickTimer.Stop();
			m_tickTimer = null;
		}
		m_tickTimer = new EcsGameTimer(target);
		m_tickTimer.Callback = new EcsGameTimer.EcsTimerCallback(PulseCallback);
		m_tickTimer.Start(1);
        target.StartEnduranceRegeneration();
	}

	/// <summary>
	/// Stop the effect on target
	/// </summary>
	public override void Stop()
	{
		base.Stop();
		if (m_tickTimer != null)
		{
			m_tickTimer.Stop();
			m_tickTimer = null;
		}
	}

	/// <summary>
	/// Sprint "pulse"
	/// </summary>
	/// <param name="callingTimer"></param>
	/// <returns></returns>
	public int PulseCallback(EcsGameTimer callingTimer)
	{
		int nextInterval;

		if (m_owner.IsMoving)
			m_idleTicks = 0;
		else m_idleTicks++;

		if (m_owner.Endurance - 5 <= 0 || m_idleTicks >= 6)
		{
			Cancel(false);
			nextInterval = 0;
		}
		else
		{
			nextInterval = Util.Random(600, 1400);
			if (m_owner.IsMoving)
			{
				int amount = 5;

				OfRaLongWindAbility ra = m_owner.GetAbility<OfRaLongWindAbility>();
				if (ra != null)
					amount = 5 - ra.GetAmountForLevel(ra.Level);

				//m_owner.Endurance -= amount;
			}
		}
		return nextInterval;
	}

	/// <summary>
	/// Called when effect must be canceled
	/// </summary>
	public override void Cancel(bool playerCancel)
	{
		base.Cancel(playerCancel);
		if (m_owner is GamePlayer)
			(m_owner as GamePlayer).Sprint(false);
	}

	/// <summary>
	/// Name of the effect
	/// </summary>
	public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.SprintEffect.Name"); } }

	/// <summary>
	/// Remaining Time of the effect in milliseconds
	/// </summary>
	public override int RemainingTime { get { return 1000; } } // always 1 for blink effect

	/// <summary>
	/// Icon to show on players, can be id
	/// </summary>
	public override ushort Icon { get { return 0x199; } }
}