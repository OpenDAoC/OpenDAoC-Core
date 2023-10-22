using System;
using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.RealmAbilities;

public class NfRaTheEmptyMindEffect : TimedEffect, IGameEffect
{
	private int Value
	{
		get
		{
			return m_value;
		}
	}

	private int m_value = 0;
	/// <summary>
	/// Constructs a new Empty Mind Effect
	/// </summary>
	public NfRaTheEmptyMindEffect(int effectiveness, Int32 duration)
		: base(duration)
	{
		m_value = effectiveness;
	}

	/// <summary>
	/// Starts the effect on the living
	/// </summary>
	/// <param name="living"></param>
	public override void Start(GameLiving living)
	{
		base.Start(living);
		m_value = Value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Body] += m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Cold] += m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Energy] += m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Heat] += m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Matter] += m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Spirit] += m_value;
		if (m_owner is GamePlayer)
			(m_owner as GamePlayer).Out.SendCharResistsUpdate(); 

		foreach (GamePlayer visiblePlayer in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			visiblePlayer.Out.SendSpellEffectAnimation(living, living, 1197, 0, false, 1);
		}
	}

	public override void Stop()
	{
		m_owner.AbilityBonus[(int)EProperty.Resist_Body] -= m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Cold] -= m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Energy] -= m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Heat] -= m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Matter] -= m_value;
		m_owner.AbilityBonus[(int)EProperty.Resist_Spirit] -= m_value;
		if (m_owner is GamePlayer)
		{
			(m_owner as GamePlayer).Out.SendCharResistsUpdate();
			(m_owner as GamePlayer).Out.SendMessage("Your clearheaded state leaves you.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}

		base.Stop();
	}

	/// <summary>
	/// Name of the effect
	/// </summary>
	public override string Name
	{
		get
		{
			return "The Empty Mind";
		}
	}

	/// <summary>
	/// Icon to show on players, can be id
	/// </summary>
	public override ushort Icon
	{
		get { return 3007; }
	}

	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo
	{
		get
		{
			var delveInfoList = new List<string>(4);
			delveInfoList.Add(string.Format("Grants the user {0} seconds of increased resistances to all magical damage by the percentage listed.", (m_duration / 1000).ToString()));
			foreach (string str in base.DelveInfo)
				delveInfoList.Add(str);

			return delveInfoList;
		}
	}
}