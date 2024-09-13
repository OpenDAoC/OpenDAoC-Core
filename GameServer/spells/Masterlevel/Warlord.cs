using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Warlord
    #region Warlord-1
    //Gamesiegeweapon - getactiondelay
    #endregion

    //shared timer 1 for 2 - shared timer 4 for 8
    #region Warlord-2/8
    [SpellHandler(eSpellType.PBAEHeal)]
    public class PBAEHealHandler : MasterlevelHandling
    {
        public override void FinishSpellCast(GameLiving target)
        {
            switch (Spell.DamageType)
            {
                case (eDamageType)((byte)1):
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
                    case (eDamageType)((byte)0):
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
                    case (eDamageType)((byte)1):
                        {
                            int healvalue = (int)m_spell.Value;
                            int heal;
                                if (target.IsAlive && !GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
                                {
                                    heal = target.ChangeHealth(target, eHealthChangeType.Spell, healvalue);
                                    if (heal != 0) player.Out.SendMessage(m_caster.Name + " heal you for " + heal + " hit point!", eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow);
                                }
                            heal = m_caster.ChangeHealth(Caster, eHealthChangeType.Spell, (int)(-m_caster.Health * 90 / 100));
                            if (heal != 0) MessageToCaster("You lose " + heal + " hit point" + (heal == 1 ? "." : "s."), eChatType.CT_Spell);

                            SendEffectAnimation(target, 0, false, 1);
                        }
                        break;
                }
            }
        }

        // constructor
        public PBAEHealHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 2
    #region Warlord-3
    [SpellHandler(eSpellType.CoweringBellow)]
    public class CoweringBellowSpellHandler : FearSpellHandler
    {
        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            var list = new List<GameLiving>();
            GameLiving target = Caster;
            foreach (GameNPC npc in target.GetNPCsInRadius((ushort)Spell.Radius))
            {
                if (npc is GameNPC && npc.Brain is ControlledMobBrain)//!(npc is NecromancerPet))
                    list.Add(npc);
            }
            return list;
        }

        public CoweringBellowSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //ML4~     //shared timer 3

    //shared timer 3
    #region Warlord-5
    [SpellHandler(eSpellType.Critical)]
    public class CriticalDamageBuff : MasterlevelDualBuffHandling
    {
        public override eProperty Property1 { get { return eProperty.CriticalSpellHitChance; } }
        public override eProperty Property2 { get { return eProperty.CriticalMeleeHitChance; } }

        public CriticalDamageBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion  
       
    //ML6~     //shared timer 4

    //shared timer 3
    #region Warlord-7
    [SpellHandler(eSpellType.CleansingAura)]
    public class CleansingAurauraSpellHandler : SpellHandler
    {
        public override bool IsOverwritable(ECSGameSpellEffect compare)
        {
            return true;
        }

        public CleansingAurauraSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 5
    #region Warlord-9
    [SpellHandler(eSpellType.EffectivenessBuff)]
    public class EffectivenessBuff : MasterlevelHandling
    {
        /// <summary>
        /// called after normal spell cast is completed and effect has to be started
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override bool HasPositiveEffect
        {
            get { return true; }
        }

        /// <summary>
        /// When an applied effect starts
        /// duration spells only
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Effectiveness += Spell.Value * 0.01;
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();
            }
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Effectiveness -= Spell.Value * 0.01;
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();
            }
            return 0;
        }

        public EffectivenessBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 5
    #region Warlord-10
    [SpellHandler(eSpellType.MLABSBuff)]
    public class MLABSBuff : MasterlevelBuffHandling
    {
        public override eProperty Property1 { get { return eProperty.ArmorAbsorption; } }

        public MLABSBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion
}
