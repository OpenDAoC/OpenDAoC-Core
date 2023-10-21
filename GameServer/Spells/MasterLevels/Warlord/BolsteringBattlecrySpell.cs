using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //shared timer 1 for 2 - shared timer 4 for 8

    [SpellHandler("PBAEHeal")]
    public class BolsteringBattlecrySpell : MasterLevelSpellHandling
    {
        public override void FinishSpellCast(GameLiving target)
        {
            switch (Spell.DamageType)
            {
                case (EDamageType)((byte)1):
                {
                    int value = (int)Spell.Value;
                    int life;
                    life = (m_caster.Health * value) / 100;
                    m_caster.Health -= life;
                }
                    break;
            }

            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            GamePlayer player = target as GamePlayer;

            if (target is GamePlayer)
            {
                switch (Spell.DamageType)
                {
                    //Warlord ML 2
                    case (EDamageType)((byte)0):
                    {
                        int mana;
                        int health;
                        int end;
                        int value = (int)Spell.Value;
                        mana = (target.MaxMana * value) / 100;
                        end = (target.MaxEndurance * value) / 100;
                        health = (target.MaxHealth * value) / 100;

                        if (target.Health + health > target.MaxHealth)
                            target.Health = target.MaxHealth;
                        else
                            target.Health += health;

                        if (target.Mana + mana > target.MaxMana)
                            target.Mana = target.MaxMana;
                        else
                            target.Mana += mana;

                        if (target.Endurance + end > target.MaxEndurance)
                            target.Endurance = target.MaxEndurance;
                        else
                            target.Endurance += end;

                        SendEffectAnimation(target, 0, false, 1);
                    }
                        break;
                    //warlord ML8
                    case (EDamageType)((byte)1):
                    {
                        int healvalue = (int)m_spell.Value;
                        int heal;
                        if (target.IsAlive && !GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
                        {
                            heal = target.ChangeHealth(target, EHealthChangeType.Spell, healvalue);
                            if (heal != 0)
                                player.Out.SendMessage(m_caster.Name + " heal you for " + heal + " hit point!",
                                    EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
                        }

                        heal = m_caster.ChangeHealth(Caster, EHealthChangeType.Spell,
                            (int)(-m_caster.Health * 90 / 100));
                        if (heal != 0)
                            MessageToCaster("You lose " + heal + " hit point" + (heal == 1 ? "." : "s."),
                                EChatType.CT_Spell);

                        SendEffectAnimation(target, 0, false, 1);
                    }
                        break;
                }
            }
        }

        // constructor
        public BolsteringBattlecrySpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}