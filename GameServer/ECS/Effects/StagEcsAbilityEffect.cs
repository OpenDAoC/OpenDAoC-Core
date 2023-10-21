using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.ECS;

public class StagEcsAbilityEffect : EcsGameAbilityEffect
{
    public StagEcsAbilityEffect(EcsGameEffectInitParams initParams, int level)
        : base(initParams)
    {
        m_level = level;
        EffectType = EEffect.Stag;
		EffectService.RequestStartEffect(this);
	}

    /// <summary>
	/// The amount of max health gained
	/// </summary>
	protected int m_amount;

    protected ushort m_originalModel;

    protected int m_level;

    public override ushort Icon { get { return 480; } }
    public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.StagEffect.Name"); } }
    public override bool HasPositiveEffect { get { return true; } }

    public override void OnStartEffect()
    {
		m_originalModel = Owner.Model;

		//TODO differentiate model between Lurikeens and other races
		if (OwnerPlayer != null)
		{
			if (OwnerPlayer.Race == (int)ERace.Lurikeen)
				OwnerPlayer.Model = 13;
			else OwnerPlayer.Model = 4;
		}


		double m_amountPercent = (m_level + 0.5 + Util.RandomDouble()) / 10; //+-5% random
		if (OwnerPlayer != null)
			m_amount = (int)(OwnerPlayer.CalculateMaxHealth(OwnerPlayer.Level, OwnerPlayer.GetModified(EProperty.Constitution)) * m_amountPercent);
		else m_amount = (int)(Owner.MaxHealth * m_amountPercent);

		Owner.BaseBuffBonusCategory[(int)EProperty.MaxHealth] += m_amount;
		Owner.Health += (int)(Owner.GetModified(EProperty.MaxHealth) * m_amountPercent);
		if (Owner.Health > Owner.MaxHealth) Owner.Health = Owner.MaxHealth;

		Owner.Emote(EEmote.StagFrenzy);

		if (OwnerPlayer != null)
		{
			OwnerPlayer.Out.SendUpdatePlayer();
			OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.StagEffect.HuntsSpiritChannel"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
		}
	}
    public override void OnStopEffect()
    {
		Owner.Model = m_originalModel;
		
		Owner.BaseBuffBonusCategory[(int)EProperty.MaxHealth] -= m_amount;
		if (Owner.IsAlive && Owner.Health > Owner.MaxHealth)
			Owner.Health = Owner.MaxHealth;

		if (OwnerPlayer != null)
		{
			OwnerPlayer.Out.SendUpdatePlayer();
			// there is no animation on end of the effect
			OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.StagEffect.YourHuntsSpiritEnds"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
		}
	}
}