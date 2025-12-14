using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_Longshot : TimedRealmAbility
    {
        public override int MaxLevel => 1;

        public AtlasOF_Longshot(DbAbility ability, int level) : base(ability, level) { }

        public override int CostForUpgrade(int level)
        {
            return 6;
        }

        public override int GetReUseDelay(int level)
        {
            return 300;
        }

        public override void Execute(GameLiving living)
        {
            if (living is not GamePlayer player)
                return;

            if (player.ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.CriticalShot.NoRangedWeapons"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            if (player.IsSitting)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.CriticalShot.MustBeStanding"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                return;
            }

            foreach (ECSGameAbilityEffect effect in player.effectListComponent.GetAbilityEffects())
            {
                if (effect.EffectType is eEffect.RapidFire or eEffect.SureShot or eEffect.Volley)
                    effect.End();
            }

            if (player.attackComponent.AttackState)
            {
                if (player.rangeAttackComponent.RangedAttackType is eRangedAttackType.Long)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CriticalShot.SwitchToRegular"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    EffectListService.GetAbilityEffectOnTarget(player, eEffect.TrueShot).End();
                }
                else
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CriticalShot.AlreadyFiring"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                return;
            }

            ECSGameEffectFactory.Create(new(player, 0, 1), this, static (in i, longshot) => new AtlasOF_LongshotECSEffect(longshot, i));
        }
    }
}
