using System;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.AfHitsBuff)]
    public class AfHitsBuffSpellHandler : SpellHandler
    {
        public override string ShortDescription => $"The target's armor and health are improved by {Math.Abs(Spell.Value)}% while within range of this aura.";

        public AfHitsBuffSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            double playerAF = 0;
            double bonusAF = 0;
            double bonusHP = 0;

            if (effect == null || effect.Owner == null)
            {
                effect.Cancel(false);
                return;
            }

            foreach (DbInventoryItem item in effect.Owner.Inventory.EquippedItems)
            {
                if (item.Object_Type >= (int)eObjectType._FirstArmor && item.Object_Type <= (int)eObjectType._LastArmor)
                {
                    playerAF += item.DPS_AF;
                }
            }

            playerAF += effect.Owner.GetModifiedFromItems(eProperty.ArmorFactor);

            if (m_spell.Value < 0)
            {
                bonusAF = ((m_spell.Value * -1) * playerAF) / 100;
                bonusHP = ((m_spell.Value * -1) * effect.Owner.MaxHealth) / 100;
            }
            else
            {
                bonusAF = m_spell.Value;
                bonusHP = m_spell.Value;
            }


            GameLiving living = effect.Owner as GameLiving;
            living.TempProperties.SetProperty("BONUS_HP", bonusHP);
            living.TempProperties.SetProperty("BONUS_AF", bonusAF);
            living.AbilityBonus[eProperty.MaxHealth] += (int)bonusHP;
            living.ItemBonus[eProperty.ArmorFactor] += (int)bonusAF;

            SendUpdates(effect.Owner);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);

            GameLiving living = effect.Owner as GameLiving;
            double bonusAF = living.TempProperties.GetProperty<double>("BONUS_AF");
            double bonusHP = living.TempProperties.GetProperty<double>("BONUS_HP");

            living.ItemBonus[eProperty.ArmorFactor] -= (int)bonusAF;
            living.AbilityBonus[eProperty.MaxHealth] -= (int)bonusHP;

            living.TempProperties.RemoveProperty("BONUS_AF");
            living.TempProperties.RemoveProperty("BONUS_HP");

            SendUpdates(effect.Owner);
            return 0;
        }

        public static void SendUpdates(GameLiving target)
        {
            GamePlayer player = target as GamePlayer;
            if (player != null)
            {
                player.Out.SendUpdatePlayer();
                player.Out.SendCharStatsUpdate();
                player.Out.SendUpdateWeaponAndArmorStats();
                player.UpdatePlayerStatus();
            }
        }
    }
}
