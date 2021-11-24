using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class DirtyTricksECSGameEffect : ECSGameAbilityEffect
    {
        public DirtyTricksECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.DirtyTricks;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 478; } }
        public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Skill.Ability.DirtyTricks.Name"); } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {

        }
        public override void OnStopEffect()
        {

        }
		public void EventHandler(AttackData attackData)
		{
			if (attackData == null) return;
			if (attackData.AttackResult != eAttackResult.HitUnstyled
				&& attackData.AttackResult != eAttackResult.HitStyle) return;
			if (attackData.Target == null) return;
			GameLiving target = attackData.Target;
			if (target == null) return;
			if (target.ObjectState != GameObject.eObjectState.Active) return;
			if (target.IsAlive == false) return;
			GameLiving attacker = Owner as GameLiving;
			if (attacker == null) return;
			if (attacker.ObjectState != GameObject.eObjectState.Active) return;
			if (attacker.IsAlive == false) return;
			if (attackData.IsOffHand) return; // only react to main hand
			if (attackData.Weapon == null) return; // no weapon attack

            DirtyTricksDetrimentalECSGameEffect dt = (DirtyTricksDetrimentalECSGameEffect)EffectListService.GetAbilityEffectOnTarget(target, eEffect.DirtyTricksDetrimental);
			if (dt == null)
			{
                new DirtyTricksDetrimentalECSGameEffect(new ECSGameEffectInitParams(target, 10000, 1));
			}
		}
	}
}

namespace DOL.GS
{
    public class DirtyTricksDetrimentalECSGameEffect : ECSGameAbilityEffect
    {
        public DirtyTricksDetrimentalECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.DirtyTricksDetrimental;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 478; } }
        public override string Name 
        {
            get
            {
                if (Owner != null && Owner is GamePlayer && (Owner as GamePlayer).Client != null)
                {
                    return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Skill.Ability.DirtyTricks.Name");
                }

                return LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "Skill.Ability.DirtyTricks.Name");
            }
        }
        public override bool HasPositiveEffect { get { return false; } }

        public override void OnStartEffect()
        {
            Owner.DebuffCategory[(int)eProperty.FumbleChance] += 35;

            if (OwnerPlayer != null)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "Skill.Ability.DirtyTricks.EffectStart"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
        public override void OnStopEffect()
        {
            Owner.DebuffCategory[(int)eProperty.FumbleChance] -= 35;

            if (OwnerPlayer != null)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "Skill.Ability.DirtyTricks.EffectCancel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}

