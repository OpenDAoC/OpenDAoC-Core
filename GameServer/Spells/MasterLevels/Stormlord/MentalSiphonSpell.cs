using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    //shared timer 2

    [SpellHandler("MentalSiphon")]
    public class MentalSiphonSpell : StormSpellHandler
    {
        // constructor
        public MentalSiphonSpell(GameLiving caster, Spell spell, SpellLine line)
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
    public class PowerDrainStormSpell : SpellHandler
    {
        public PowerDrainStormSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }

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
                ((GamePlayer)target).Out.SendMessage(m_caster.Name + " steals you " + mana + " points of power!",
                    EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
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
}