using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class TripleWieldECSGameEffect : ECSGameAbilityEffect
    {
        public override ushort Icon => 475;
        public override string Name => LanguageMgr.GetTranslation(Owner is GamePlayer playerOwner ? playerOwner.Client : null, "Effects.TripleWieldEffect.Name");
        public override bool HasPositiveEffect => true;

        public TripleWieldECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.TripleWield;
            EffectService.RequestStartEffect(this);
        }

        public override void OnStartEffect()
        {
            foreach (GamePlayer playerInRadius in Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                playerInRadius.Out.SendSpellEffectAnimation(Owner, Owner, 7102, 0, false, 1);
                playerInRadius.Out.SendSpellCastAnimation(Owner, Icon, 0);
            }
        }

        public override void OnStopEffect() { }

        public void EventHandler(AttackData attackData)
        {
            if (attackData?.Weapon == null || attackData.AttackResult is not eAttackResult.HitUnstyled and not eAttackResult.HitStyle)
                return;

            GameLiving target = attackData.Target;

            if (target == null || target.ObjectState is not GameObject.eObjectState.Active || !target.IsAlive)
                return;

            GameLiving attacker = Owner;

            if (attacker == null || attacker.ObjectState is not GameObject.eObjectState.Active || !attacker.IsAlive)
                return;

            double damage = attackData.Attacker.WeaponDamage(attackData.Weapon) * attackData.Interval * 0.0005; // 50% of weapon DPS damage add.
            double damageResisted = damage * target.GetResist(eDamageType.Body) * -0.01;

            AttackData ad = new()
            {
                Attacker = attacker,
                Target = target,
                Damage = (int) (damage + damageResisted),
                Modifier = (int) damageResisted,
                DamageType = eDamageType.Body,
                AttackType = AttackData.eAttackType.MeleeOneHand,
                AttackResult = eAttackResult.HitUnstyled,
                Interval = attackData.Interval
            };

            if (attacker is GamePlayer playerAttacker)
            {
                playerAttacker.Out.SendMessage(LanguageMgr.GetTranslation(playerAttacker.Client, "Effects.TripleWieldEffect.MBHitsExtraDamage", target.GetName(0, false), ad.Damage), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

                if (target is GamePlayer playerTarget)
                    playerTarget.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.TripleWieldEffect.XMBExtraDamageToYou", attacker.GetName(0, false), ad.Damage), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            }

            target.OnAttackedByEnemy(ad);
            attacker.DealDamage(ad);

            foreach (GamePlayer playerInRadius in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                playerInRadius.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x0A, target.HealthPercent);
        }
    }
}
