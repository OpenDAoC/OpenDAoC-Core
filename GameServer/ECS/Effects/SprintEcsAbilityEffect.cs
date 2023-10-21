using System;
using Core.GS.PacketHandler;
using Core.GS.RealmAbilities;
using Core.Language;

namespace Core.GS.ECS;

public class SprintEcsAbilityEffect : EcsGameAbilityEffect
{
    public SprintEcsAbilityEffect(EcsGameEffectInitParams initParams)
        : base(initParams) 
	{
		EffectType = EEffect.Sprint;
		NextTick = GameLoop.GameLoopTime + 1;
		EffectService.RequestStartEffect(this);
	}

	/// <summary>
	/// The amount of timer ticks player was not moving
	/// </summary>
	int m_idleTicks = 0;

	public override ushort Icon { get { return 0x199; } }
    public override string Name { get { return LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.SprintEffect.Name"); } }
    public override bool HasPositiveEffect { get { return true; } }
    public override long GetRemainingTimeForClient(){ return 1000; }

    public override void OnStartEffect()
    {
		if (OwnerPlayer != null)
		{
			int regen = OwnerPlayer.GetModified(EProperty.EnduranceRegenerationRate);
			var endchant = OwnerPlayer.GetModified(EProperty.FatigueConsumption);
			var cost = -5 + regen;
			if (endchant > 1) cost = (int)Math.Ceiling(cost * endchant * 0.01);
			OwnerPlayer.Endurance += cost;

			OwnerPlayer.Out.SendUpdateMaxSpeed();
			OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Sprint.PrepareSprint"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			Owner.StartEnduranceRegeneration();
		}
	}
    public override void OnStopEffect()
    {
		if (OwnerPlayer != null)
		{
			OwnerPlayer.Out.SendUpdateMaxSpeed();
			OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Sprint.NoLongerReady"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
	}
    public override void OnEffectPulse()
    {
		int nextInterval;

		if (Owner.IsMoving)
			m_idleTicks = 0;
		else m_idleTicks++;

		if (Owner.Endurance - 5 <= 0 || m_idleTicks >= 6)
		{
			EffectService.RequestImmediateCancelEffect(this);
			nextInterval = 0;
		}
		else
		{
			nextInterval = Util.Random(600, 1400);
			if (Owner.IsMoving)
			{
				int amount = 5;

				OfRaLongWindAbility ra = Owner.GetAbility<OfRaLongWindAbility>();
				if (ra != null)
					amount = 5 - ra.GetAmountForLevel(ra.Level);

				//m_owner.Endurance -= amount;
			}
		}
		NextTick += nextInterval;
	}
}