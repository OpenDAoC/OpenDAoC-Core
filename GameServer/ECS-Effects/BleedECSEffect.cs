using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class BleedECSEffect : DamageOverTimeECSGameEffect
    {
        private int _nextTickDamage;

        public BleedECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            _nextTickDamage = (int) SpellHandler.Spell.Damage;
            EffectService.RequestStartEffect(this);
        }

        public override bool IsBetterThan(ECSGameSpellEffect effect)
        {
            return effect is BleedECSEffect otherBleedEffect && _nextTickDamage > otherBleedEffect._nextTickDamage;
        }

        public override void OnEffectPulse()
        {
            if (!Owner.IsAlive)
                EffectService.RequestImmediateCancelEffect(this);

            if (SpellHandler is not StyleBleeding bleedHandler)
                return;

            if (OwnerPlayer != null)
            {
                bleedHandler.MessageToLiving(Owner, bleedHandler.Spell.Message1, eChatType.CT_YouWereHit);
                Message.SystemToArea(Owner, Util.MakeSentence(bleedHandler.Spell.Message2, Owner.GetName(0, false)), eChatType.CT_YouHit, Owner);
            }

            AttackData ad = bleedHandler.CalculateDamageToTarget(Owner);
            ad.Modifier = _nextTickDamage * SpellHandler.Target.GetResist(SpellHandler.Spell.DamageType) / -100;
            ad.Damage = _nextTickDamage + ad.Modifier;
            bleedHandler.SendDamageMessages(ad);

            foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);

            ad.Target.OnAttackedByEnemy(ad);
            ad.Attacker.DealDamage(ad);

            if (_nextTickDamage > 1)
                _nextTickDamage--;

            FinalizeEffectPulse();
        }
    }
}
