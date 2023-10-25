using System.Collections.Generic;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Skills;

namespace Core.GS.Effects.Old;

public class StealthEffect : StaticEffect, IGameEffect
{
	/// <summary>
	/// The owner of the effect
	/// </summary>
	GamePlayer m_player;

	/// <summary>
	/// Start the stealth on player
	/// </summary>
	public void Start(GamePlayer player)
	{
		m_player = player;
		player.EffectList.Add(this);
	}

	/// <summary>
	/// Stop the effect on target
	/// </summary>
	public override void Stop()
	{
		if (m_player.HasAbility(AbilityConstants.Camouflage))
		{
			CamouflageEcsAbilityEffect camouflage = (CamouflageEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(m_player, EEffect.Camouflage);
			if (camouflage!=null)
				EffectService.RequestImmediateCancelEffect(camouflage, false);
		}
		m_player.EffectList.Remove(this);
	}

	/// <summary>
	/// Called when effect must be canceled
	/// </summary>
	public override void Cancel(bool playerCancel)
	{
		m_player.Stealth(false);
	}

	/// <summary>
	/// Name of the effect
	/// </summary>
	public override string Name { get { return LanguageMgr.GetTranslation(m_player.Client, "Effects.StealthEffect.Name"); } }

	/// <summary>
	/// Remaining Time of the effect in milliseconds
	/// </summary>
	public override int RemainingTime { get { return 0; } }

	/// <summary>
	/// Icon to show on players, can be id
	/// </summary>
	public override ushort Icon { get { return 0x193; } }

	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo { get { return new string[0]; } }
}