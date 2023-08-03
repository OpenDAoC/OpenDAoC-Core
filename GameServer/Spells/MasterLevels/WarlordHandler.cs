using System.Collections.Generic;
using System.Reflection;
using log4net;
using System;
using System.Collections;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS;
using DOL.Events;
using System.Collections.Specialized;

namespace DOL.GS.Spells
{    
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Warlord
    #region Warlord-1
    //Gamesiegeweapon - getactiondelay
    #endregion

    //shared timer 1 for 2 - shared timer 4 for 8
    #region Warlord-2/8
    [SpellHandlerAttribute("PBAEHeal")]
    public class PbaeHealHandler : MasterLevelHandling
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

        public override void OnDirectEffect(GameLiving target, double effectiveness)
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
                                    if (heal != 0) player.Out.SendMessage(m_caster.Name + " heal you for " + heal + " hit point!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
                                }
                            heal = m_caster.ChangeHealth(Caster, EHealthChangeType.Spell, (int)(-m_caster.Health * 90 / 100));
                            if (heal != 0) MessageToCaster("You lose " + heal + " hit point" + (heal == 1 ? "." : "s."), EChatType.CT_Spell);

                            SendEffectAnimation(target, 0, false, 1);
                        }
                        break;
                }
            }
        }

        // constructor
        public PbaeHealHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 2
    #region Warlord-3
    [SpellHandlerAttribute("CoweringBellow")]
    public class CoweringBellowHandler : FearHandler
    {
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            var list = new List<GameLiving>();
            GameLiving target = Caster;
            foreach (GameNpc npc in target.GetNPCsInRadius((ushort)Spell.Radius))
            {
                if (npc is GameNpc && npc.Brain is ControlledNpcBrain)//!(npc is NecromancerPet))
                    list.Add(npc);
            }
            return list;
        }

        public CoweringBellowHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //ML4~     //shared timer 3

    //shared timer 3
    #region Warlord-5
    [SpellHandlerAttribute("Critical")]
    public class CriticalDamageBuffHandler : MasterLevelDualBuffHandling
    {
        public override EProperty Property1 { get { return EProperty.CriticalSpellHitChance; } }
        public override EProperty Property2 { get { return EProperty.CriticalMeleeHitChance; } }

        public CriticalDamageBuffHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion  
       
    //ML6~     //shared timer 4

    //shared timer 3
    #region Warlord-7
    [SpellHandlerAttribute("CleansingAura")]
    public class CleansingAuraHandler : SpellHandler
    {
        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return true;
        }

        public CleansingAuraHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 5
    #region Warlord-9
    [SpellHandlerAttribute("EffectivenessBuff")]
    public class EffectivenessBuffHandler : MasterLevelHandling
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

        public EffectivenessBuffHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 5
    #region Warlord-10
    [SpellHandlerAttribute("MLABSBuff")]
    public class MlAbsBuffHandler : MasterLevelBuffHandling
    {
        public override EProperty Property1 { get { return EProperty.ArmorAbsorption; } }

        public MlAbsBuffHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion
}