using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("SiegeDirectDamage")]
    public class SiegeDirectDamageSpellHandler : DirectDamageSpellHandler
    {
        public SiegeDirectDamageSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <summary>
        /// Calculates chance of spell getting resisted
        /// </summary>
        /// <param name="target">the target of the spell</param>
        /// <returns>chance that spell will be resisted for specific target</returns>
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override int CalculateToHitChance(GameLiving target)
        {
            return 100;
        }

        public override bool CasterIsAttacked(GameLiving attacker)
        {
            return false;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            min = 1;
            max = 1;
        }

        public override AttackData CalculateDamageToTarget(GameLiving target)
        {
            AttackData ad = base.CalculateDamageToTarget(target);
            if (target is GamePlayer)
            {
                GamePlayer player = target as GamePlayer;
                int id = player.CharacterClass.ID;
                //50% reduction for tanks
                if (id == (int)eCharacterClass.Armsman || id == (int)eCharacterClass.Warrior || id == (int)eCharacterClass.Hero)
                    ad.Damage /= 2;
                //3000 spec
                //ram protection
                //lvl 0 50%
                //lvl 1 60%
                //lvl 2 70%
                //lvl 3 80%
                if (player.IsRiding && player.Steed is GameSiegeRam)
                {
                    ad.Damage = (int)((double)ad.Damage * (1.0 - (50.0 + (double)player.Steed.Level * 10.0) / 100.0));
                }
            }
            return ad;
        }

        public override void SendDamageMessages(AttackData ad)
        {
            string modmessage = "";
            if (ad.Modifier > 0)
                modmessage = " (+" + ad.Modifier + ")";
            if (ad.Modifier < 0)
                modmessage = " (" + ad.Modifier + ")";

            if (Caster is GameSiegeWeapon)
            {
                GameSiegeWeapon siege = (Caster as GameSiegeWeapon);
                if (siege.Owner != null)
                {
                    siege.Owner.Out.SendMessage(string.Format("You hit {0} for {1}{2} damage!", ad.Target.GetName(0, false), ad.Damage, modmessage), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                }
            }

            if (Caster is GamePlayer p)
            {
                p.Out.SendMessage(string.Format("You hit {0} for {1}{2} damage!", ad.Target.GetName(0, false), ad.Damage, modmessage), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            }
        }

        public override void DamageTarget(AttackData ad, bool showEffectAnimation, int attackResult)
        {
            if (Caster is GameSiegeWeapon)
            {
                GameSiegeWeapon siege = (Caster as GameSiegeWeapon);
                if (siege.Owner != null)
                {
                    ad.Attacker = siege.Owner;
                }
            }
            base.DamageTarget(ad, showEffectAnimation, attackResult);
        }
    }
}
