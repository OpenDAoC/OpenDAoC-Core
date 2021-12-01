using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class StagECSGameEffect : ECSGameAbilityEffect
    {
        public StagECSGameEffect(ECSGameEffectInitParams initParams, int level)
            : base(initParams)
        {
            m_level = level;
            EffectType = eEffect.Stag;
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
				if (OwnerPlayer.Race == (int)eRace.Lurikeen)
					OwnerPlayer.Model = 13;
				else OwnerPlayer.Model = 4;
			}


			double m_amountPercent = (m_level + 0.5 + Util.RandomDouble()) / 10; //+-5% random
			if (OwnerPlayer != null)
				m_amount = (int)(OwnerPlayer.CalculateMaxHealth(OwnerPlayer.Level, OwnerPlayer.GetModified(eProperty.Constitution)) * m_amountPercent);
			else m_amount = (int)(Owner.MaxHealth * m_amountPercent);

			Owner.BaseBuffBonusCategory[(int)eProperty.MaxHealth] += m_amount;
			Owner.Health += (int)(Owner.GetModified(eProperty.MaxHealth) * m_amountPercent);
			if (Owner.Health > Owner.MaxHealth) Owner.Health = Owner.MaxHealth;

			Owner.Emote(eEmote.StagFrenzy);

			if (OwnerPlayer != null)
			{
				OwnerPlayer.Out.SendUpdatePlayer();
				OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.StagEffect.HuntsSpiritChannel"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
			}
		}
        public override void OnStopEffect()
        {
			Owner.Model = m_originalModel;

			double m_amountPercent = m_amount / Owner.GetModified(eProperty.MaxHealth);
			int playerHealthPercent = Owner.HealthPercent;
			Owner.BaseBuffBonusCategory[(int)eProperty.MaxHealth] -= m_amount;
			if (Owner.IsAlive)
				Owner.Health = (int)Math.Max(1, 0.01 * Owner.MaxHealth * playerHealthPercent);

			if (OwnerPlayer != null)
			{
				OwnerPlayer.Out.SendUpdatePlayer();
				// there is no animation on end of the effect
				OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.StagEffect.YourHuntsSpiritEnds"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
			}
		}
    }
}
