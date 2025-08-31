using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class DirtyTricksECSGameEffect : ECSGameAbilityEffect
    {
        public DirtyTricksECSGameEffect(in ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.DirtyTricks;
        }

        public override ushort Icon { get { return 478; } }
        public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Skill.Ability.DirtyTricks.Name"); } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            foreach (GamePlayer plyr in Owner.GetPlayersInRadius(500))
            {
                plyr.Out.SendSoundEffect(163, 0, 0, 0, 0, 0);    
            }
            

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
                ECSGameEffectFactory.Create(new(target, 10000, 1), static (in ECSGameEffectInitParams i) => new DirtyTricksDetrimentalECSGameEffect(i));
			}
		}
	}
}

namespace DOL.GS
{
    public class DirtyTricksDetrimentalECSGameEffect : ECSGameAbilityEffect
    {
        public DirtyTricksDetrimentalECSGameEffect(in ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.DirtyTricksDetrimental;
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
            Owner.DebuffCategory[eProperty.FumbleChance] += 35;

            if (OwnerPlayer != null)
            {
                // Message: "{0} flings a cloud of dirt in your eyes!"
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "Effects.DirtyTricks.EffectStart"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                
                //todo Identify the player triggering the effect as well as the effect owner
                // Message: "{0} throws dirt in {1}'s eyes!"
                // Message.SystemToArea(Owner, LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "Effects.DirtyTricks.AreaEffectStart", OwnerPlayer.Name, Owner.GetName(0, false)), eChatType.CT_System);
            }

        }
        public override void OnStopEffect()
        {
            Owner.DebuffCategory[eProperty.FumbleChance] -= 35;

            if (OwnerPlayer != null)
            {
                // Message: "You can see clearly again."
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "Effects.DirtyTricks.EffectCancel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                // Message: "{0} can see clearly again."
                Message.SystemToArea(Owner, LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "Effects.DirtyTricks.AreaEffectCancel", Owner.GetName(0, true)), eChatType.CT_System);
            }

        }
    }
}

