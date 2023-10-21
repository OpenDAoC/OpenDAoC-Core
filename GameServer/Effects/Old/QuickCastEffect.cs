using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Effects.Old;

public class XQuickCastEffect : TimedEffect, IGameEffect
{
    public const int DURATION = 3000;
    public XQuickCastEffect() : base(DURATION)
    {
    }

    /// <summary>
    /// Start the quickcast on player
    /// </summary>
    public override void Start(GameLiving living)
	{
		base.Start(living);
		if (m_owner is GamePlayer)
			(m_owner as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((m_owner as GamePlayer).Client, "Effects.QuickCastEffect.YouActivatedQC"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		m_owner.TempProperties.RemoveProperty(Spells.SpellHandler.INTERRUPT_TIMEOUT_PROPERTY);
        
	}

    /// <summary>
    /// Called when effect must be canceled
    /// </summary>
    public override void Cancel(bool playerCancel)
	{
		base.Cancel(playerCancel);
		if (m_owner is GamePlayer)
			(m_owner as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((m_owner as GamePlayer).Client, "Effects.QuickCastEffect.YourNextSpellNoQCed"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	/// <summary>
	/// Name of the effect
	/// </summary>
	public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.QuickCastEffect.Name"); } }

	/// <summary>
	/// Remaining Time of the effect in milliseconds
	/// </summary>
	public override int RemainingTime { get { return 1000; } }

	/// <summary>
	/// Icon to show on players, can be id
	/// </summary>
	public override ushort Icon { get { return 0x0190; } }

	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo
	{
		get
		{
			var delveInfoList = new List<string>();
            delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.Quickcast.DelveInfo"));

            return delveInfoList;
		}
	}
}