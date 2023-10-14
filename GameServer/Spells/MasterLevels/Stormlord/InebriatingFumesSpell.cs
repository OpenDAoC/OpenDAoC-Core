using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    //shared timer 1

    [SpellHandler("InebriatingFumes")]
    public class InebriatingFumesSpell : StormSpellHandler
    {
        // constructor
        public InebriatingFumesSpell(GameLiving caster, Spell spell, SpellLine line)
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
        public override EProperty Property1
        {
            get { return EProperty.Dexterity; }
        }

        public override EProperty Property2
        {
            get { return EProperty.Quickness; }
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            neweffect.Start(target);

            if (target is GamePlayer)
                ((GamePlayer)target).Out.SendMessage("Your dexterity and quickness decreased!", EChatType.CT_YouWereHit,
                    EChatLoc.CL_SystemWindow);

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
        public StormDexQuickDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}