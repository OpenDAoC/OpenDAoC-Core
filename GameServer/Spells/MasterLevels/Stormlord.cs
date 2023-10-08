/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Stormlord
    //shared timer 1
    #region Stormlord-1
    [SpellHandler("DazzlingArray")]
    public class DazzlingArraySpellHandler : StormSpellHandler
    {
        // constructor
        public DazzlingArraySpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            //Construct a new storm.
            storm = new GameStorm();
            storm.Realm = caster.Realm;
            storm.X = caster.X;
            storm.Y = caster.Y;
            storm.Z = caster.Z;
            storm.CurrentRegionID = caster.CurrentRegionID;
            storm.Heading = caster.Heading;
            storm.Owner = (GamePlayer)caster;
            storm.Movable = true;

            // Construct the storm spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7210;
            dbs.ClientEffect = 7210;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Realm";
            dbs.Radius = 0;
            dbs.Type = ESpellType.StormMissHit.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth; // should be 4
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    [SpellHandler("StormMissHit")]
    public class StormMissHit : MasterlevelBuffHandling
    {
        public override EProperty Property1 { get { return EProperty.MissHit; } }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            neweffect.Start(target);

            if (target is GamePlayer)
                ((GamePlayer)target).Out.SendMessage("You're harder to hit!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);

        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {

            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public StormMissHit(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Stormlord-2
    [SpellHandler("VacuumVortex")]
    public class VacuumVortexSpellHandler : SpellHandler
    {
        /// <summary>
        /// Execute direct damage spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override IList<GameLiving> SelectTargets(GameObject CasterTarget)
        {
            
            var list = new List<GameLiving>(8);
            foreach (GameNPC storms in Caster.GetNPCsInRadius(350))
            {
                if ((storms is GameStorm) && (GameServer.ServerRules.IsSameRealm(storms, Caster, true))) list.Add(storms);
            }
            return list;
        }
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override void OnDirectEffect(GameLiving target)
        {
            //base.OnDirectEffect(target, effectiveness);
            var targets = SelectTargets(Caster);

            if (targets == null) return;

            foreach (GameStorm targetStorm in targets)
            {
                if (targetStorm.Movable)
                {
                    int range = Util.Random(0, 750);
                    double angle = Util.RandomDouble() * 2 * Math.PI;
                    targetStorm.WalkTo(new Point3D(targetStorm.X + (int) (range * Math.Cos(angle)), targetStorm.Y + (int) (range * Math.Sin(angle)), targetStorm.Z), targetStorm.MaxSpeed);
                }
            }
        }

        public VacuumVortexSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 2
    #region Stormlord-3
    [SpellHandler("EnervatingGas")]
    public class EnervatingGasSpellHandler : StormSpellHandler
    {
        // constructor
        public EnervatingGasSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            //Construct a new storm.
            storm = new GameStorm();
            storm.Realm = caster.Realm;
            storm.X = caster.X;
            storm.Y = caster.Y;
            storm.Z = caster.Z;
            storm.CurrentRegionID = caster.CurrentRegionID;
            storm.Heading = caster.Heading;
            storm.Owner = (GamePlayer)caster;
            storm.Movable = true;



            // Construct the storm spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7273;
            dbs.ClientEffect = 7273;
            dbs.Damage = Math.Abs(spell.Damage);
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.StormEnduDrain.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth; //should be 2
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    [SpellHandler("StormEnduDrain")]
    public class StormEndudrain : SpellHandler
    {

        public StormEndudrain(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);

            neweffect.Start(target);

            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            //spell damage should 25;
            int end = (int)(Spell.Damage);
            target.ChangeEndurance(target, EEnduranceChangeType.Spell, (-end));

            if (target is GamePlayer)
                ((GamePlayer)target).Out.SendMessage(" You lose " + end + " endurance!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
            (m_caster as GamePlayer).Out.SendMessage("" + target.Name + " loses " + end + " endurance!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {

            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
    }
    #endregion

    //shared timer 1
    #region Stormlord-4
    [SpellHandler("InebriatingFumes")]
    public class InebriatingFumesSpellHandler : StormSpellHandler
    {
        // constructor
        public InebriatingFumesSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            //Construct a new storm.
            storm = new GameStorm();
            storm.Realm = caster.Realm;
            storm.X = caster.X;
            storm.Y = caster.Y;
            storm.Z = caster.Z;
            storm.CurrentRegionID = caster.CurrentRegionID;
            storm.Heading = caster.Heading;
            storm.Owner = (GamePlayer)caster;
            storm.Movable = true;

            // Construct the storm spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7258;
            dbs.ClientEffect = 7258;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.Damage;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.StormDexQuickDebuff.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth; // should be 2
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    /// <summary>
    /// Dex/Qui stat specline debuff
    /// </summary>
    [SpellHandler("StormDexQuickDebuff")]
    public class StormDexQuickDebuff : DualStatDebuff
    {
        public override EProperty Property1 { get { return EProperty.Dexterity; } }
        public override EProperty Property2 { get { return EProperty.Quickness; } }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            neweffect.Start(target);

            if (target is GamePlayer)
                ((GamePlayer)target).Out.SendMessage("Your dexterity and quickness decreased!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);

        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {

            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public StormDexQuickDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 2
    #region Stormlord-5
    [SpellHandler("MentalSiphon")]
    public class MentalSiphonSpellHandler : StormSpellHandler
    {
        // constructor
        public MentalSiphonSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            //Construct a new storm.
            storm = new GameStorm();
            storm.Realm = caster.Realm;
            storm.X = caster.X;
            storm.Y = caster.Y;
            storm.Z = caster.Z;
            storm.CurrentRegionID = caster.CurrentRegionID;
            storm.Heading = caster.Heading;
            storm.Owner = (GamePlayer)caster;
            storm.Movable = true;

            // Construct the storm spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7303;
            dbs.ClientEffect = 7303;
            dbs.Damage = Math.Abs(spell.Damage);
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.PowerDrainStorm.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth; // should be 2
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }

    [SpellHandler("PowerDrainStorm")]
    public class PowerDrainStormSpellHandler : SpellHandler
    {
        public PowerDrainStormSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            neweffect.Start(target);
            int mana = (int)(Spell.Damage);
            target.ChangeMana(target, EPowerChangeType.Spell, (-mana));

            if (target is GamePlayer)
            {
                ((GamePlayer)target).Out.SendMessage(m_caster.Name + " steals you " + mana + " points of power!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
            }

            StealMana(target, mana);
            // target.StartInterruptTimer(SPELL_INTERRUPT_DURATION, AttackData.eAttackType.Spell, Caster);
        }


        public virtual void StealMana(GameLiving target, int mana)
        {
            if (!m_caster.IsAlive) return;
            m_caster.ChangeMana(target, EPowerChangeType.Spell, mana);
            SendCasterMessage(target, mana);

        }


        public virtual void SendCasterMessage(GameLiving target, int mana)
        {
            MessageToCaster(string.Format("You steal {0} for {1} power!", target.Name, mana), EChatType.CT_YouHit);
            if (mana > 0)
            {
                MessageToCaster("You steal " + mana + " power points" + (mana == 1 ? "." : "s."), EChatType.CT_Spell);
            }
            //else
            //{
            //   MessageToCaster("You cannot absorb any more power.", eChatType.CT_SpellResisted);
            //}
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }
    }

    #endregion

    //no shared timer
    #region Stormlord-6
    [SpellHandler("FocusingWinds")]
    public class FocusingWindsSpellHandler : SpellHandler
    {
        private GameSpellEffect m_effect;
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            m_effect = effect;
            if (effect.Owner is GameStorm)
            {
                GameStorm targetStorm = effect.Owner as GameStorm;
                targetStorm.Movable = false;
                MessageToCaster("Now the vortex of this storm is locked!", EChatType.CT_YouWereHit);
                GameEventMgr.AddHandler(m_caster, GameLivingEvent.Moving, new CoreEventHandler(LivingMoves));
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner is GameStorm)
            {
                GameStorm targetStorm = effect.Owner as GameStorm;
                targetStorm.Movable = true;
                GameEventMgr.RemoveHandler(m_caster, GameLivingEvent.Moving, new CoreEventHandler(LivingMoves));
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        public void LivingMoves(CoreEvent e, object sender, EventArgs args)
        {
            GameLiving player = sender as GameLiving;
            if (player == null) return;
            if (e == GameLivingEvent.Moving)
            {
                MessageToCaster("You are moving. Your concentration fades", EChatType.CT_SpellExpires);
                OnEffectExpires(m_effect, true);
                return;
            }
        }
        public FocusingWindsSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 1
    #region Stormlord-7
    [SpellHandler("ChokingVapors")]
    public class ChokingVaporsSpellHandler : StormSpellHandler
    {
        // constructor
        public ChokingVaporsSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            //Construct a new storm.
            storm = new GameStorm();
            storm.Realm = caster.Realm;
            storm.X = caster.X;
            storm.Y = caster.Y;
            storm.Z = caster.Z;
            storm.CurrentRegionID = caster.CurrentRegionID;
            storm.Heading = caster.Heading;
            storm.Owner = (GamePlayer)caster;
            storm.Movable = true;

            // Construct the storm spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7223;
            dbs.ClientEffect = 7223;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.StormStrConstDebuff.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth; // should be 2
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    /// <summary>
    /// Str/Con stat specline debuff
    /// </summary>
    [SpellHandler("StormStrConstDebuff")]
    public class StormStrConstDebuff : DualStatDebuff
    {
        public override EProperty Property1 { get { return EProperty.Strength; } }
        public override EProperty Property2 { get { return EProperty.Constitution; } }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            neweffect.Start(target);

            if (target is GamePlayer)
                ((GamePlayer)target).Out.SendMessage("Your strenght and constitution decreased!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);

        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {

            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public StormStrConstDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 1
    #region Stormlord-8
    [SpellHandler("SenseDullingCloud")]
    public class SenseDullingCloudSpellHandler : StormSpellHandler
    {
        // constructor
        public SenseDullingCloudSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            //Construct a new storm.
            storm = new GameStorm();
            storm.Realm = caster.Realm;
            storm.X = caster.X;
            storm.Y = caster.Y;
            storm.Z = caster.Z;
            storm.CurrentRegionID = caster.CurrentRegionID;
            storm.Heading = caster.Heading;
            storm.Owner = (GamePlayer)caster;
            storm.Movable = true;

            // Construct the storm spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7305;
            dbs.ClientEffect = 7305;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.StormAcuityDebuff.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth; // should be 2
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    /// <summary>
    /// Acuity stat baseline debuff
    /// </summary>
    [SpellHandler("StormAcuityDebuff")]
    public class StormAcuityDebuff : SingleStatDebuff
    {
        public override EProperty Property1
        {

            get
            {
                EProperty temp = EProperty.Acuity;
                if (Target.Realm == ERealm.Albion) temp = EProperty.Intelligence;
                if (Target.Realm == ERealm.Midgard) temp = EProperty.Piety;
                if (Target.Realm == ERealm.Hibernia) temp = EProperty.Intelligence;

                return temp;
            }
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            neweffect.Start(target);

            if (target is GamePlayer)
                ((GamePlayer)target).Out.SendMessage("Your acuity decreased!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);

        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {

            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public StormAcuityDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Stormlord-9
    [SpellHandler("EnergyTempest")]
    public class EnergyTempestSpellHandler : StormSpellHandler
    {
        // constructor
        public EnergyTempestSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            //Construct a new storm.
            storm = new GameStorm();
            storm.Realm = caster.Realm;
            storm.X = caster.X;
            storm.Y = caster.Y;
            storm.Z = caster.Z;
            storm.CurrentRegionID = caster.CurrentRegionID;
            storm.Heading = caster.Heading;
            storm.Owner = (GamePlayer)caster;
            storm.Movable = true;

            // Construct the storm spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7216;
            dbs.ClientEffect = 7216;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.StormEnergyTempest.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    [SpellHandler("StormEnergyTempest")]
    public class StormEnergyTempest : SpellHandler
    {
        /// <summary>
        /// Calculates the base 100% spell damage which is then modified by damage variance factors
        /// </summary>
        /// <returns></returns>
        public override double CalculateDamageBase(GameLiving target)
        {
            GamePlayer player = Caster as GamePlayer;

            // % damage procs
            if (Spell.Damage < 0)
            {
                double spellDamage = 0;

                if (player != null)
                {
                    // This equation is used to simulate live values - Tolakram
                    spellDamage = (target.MaxHealth * -Spell.Damage * .01) / 2.5;
                }

                if (spellDamage < 0)
                    spellDamage = 0;

                return spellDamage;
            }

            return base.CalculateDamageBase(target);
        }

        public override double DamageCap(double effectiveness)
        {
            if (Spell.Damage < 0)
            {
                return (Target.MaxHealth * -Spell.Damage * .01) * 3.0 * effectiveness;
            }

            return base.DamageCap(effectiveness);
        }
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            neweffect.Start(target);


            // calc damage
            AttackData ad = CalculateDamageToTarget(target);
            SendDamageMessages(ad);
            DamageTarget(ad, true);            
            target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {

            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public StormEnergyTempest(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //ML 10 Arcing Power - already handled in another area
}
