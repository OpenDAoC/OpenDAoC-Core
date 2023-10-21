using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    [SpellHandler("AfHitsBuff")]
    public class AfHitsBuffSpell : SpellHandler
    {
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
                if (item.Object_Type >= (int)EObjectType._FirstArmor && item.Object_Type <= (int)EObjectType._LastArmor)
                {
                    playerAF += item.DPS_AF;
                }
            }

            playerAF += effect.Owner.GetModifiedFromItems(EProperty.ArmorFactor);

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
            living.AbilityBonus[(int)EProperty.MaxHealth] += (int)bonusHP;
            living.ItemBonus[(int)EProperty.ArmorFactor] += (int)bonusAF;

            SendUpdates(effect.Owner);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);

            GameLiving living = effect.Owner as GameLiving;
            double bonusAF = living.TempProperties.GetProperty<double>("BONUS_AF");
            double bonusHP = living.TempProperties.GetProperty<double>("BONUS_HP");

            living.ItemBonus[(int)EProperty.ArmorFactor] -= (int)bonusAF;
            living.AbilityBonus[(int)EProperty.MaxHealth] -= (int)bonusHP;

            living.TempProperties.RemoveProperty("BONUS_AF");
            living.TempProperties.RemoveProperty("BONUS_HP");

            SendUpdates(effect.Owner);
            return 0;
        }
        public void SendUpdates(GameLiving target)
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

        public AfHitsBuffSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}
