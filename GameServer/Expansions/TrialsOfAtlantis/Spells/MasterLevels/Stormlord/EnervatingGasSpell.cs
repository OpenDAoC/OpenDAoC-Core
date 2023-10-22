using System;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Scripts;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

//shared timer 2

[SpellHandler("EnervatingGas")]
public class EnervatingGasSpell : StormSpellHandler
{
    // constructor
    public EnervatingGasSpell(GameLiving caster, Spell spell, SpellLine line)
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

    public StormEndudrain(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }

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
            ((GamePlayer)target).Out.SendMessage(" You lose " + end + " endurance!", EChatType.CT_YouWereHit,
                EChatLoc.CL_SystemWindow);
        (m_caster as GamePlayer).Out.SendMessage("" + target.Name + " loses " + end + " endurance!",
            EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
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