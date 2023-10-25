using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.Expansions.TrialsOfAtlantis.MasterLevels;

//shared timer 5 for ml2 - shared timer 3 for ml8

[SpellHandler("PBAEDamage")]
public class PrimalAgonySpell : MasterLevelSpellHandling
{
    // constructor
    public PrimalAgonySpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }

    public override void FinishSpellCast(GameLiving target)
    {
        m_caster.Mana -= PowerCost(target);
        //For Banelord ML 8, it drains Life from the Caster
        if (Spell.Damage > 0)
        {
            int chealth;
            chealth = (m_caster.Health * (int)Spell.Damage) / 100;

            if (m_caster.Health < chealth)
                chealth = 0;

            m_caster.Health -= chealth;
        }

        base.FinishSpellCast(target);
    }

    public override void OnDirectEffect(GameLiving target)
    {
        if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

        GamePlayer player = target as GamePlayer;
        if (target is GamePlayer)
        {
            int mana;
            int health;
            int end;

            int value = (int)Spell.Value;
            mana = (player.Mana * value) / 100;
            end = (player.Endurance * value) / 100;
            health = (player.Health * value) / 100;

            //You don't gain RPs from this Spell
            if (player.Health < health)
                player.Health = 1;
            else
                player.Health -= health;

            if (player.Mana < mana)
                player.Mana = 1;
            else
                player.Mana -= mana;

            if (player.Endurance < end)
                player.Endurance = 1;
            else
                player.Endurance -= end;

            GameSpellEffect effect2 = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
            if (effect2 != null)
            {
                effect2.Cancel(true);
                return;
            }

            foreach (GamePlayer ply in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                SendEffectAnimation(player, 0, false, 1);
            }

            player.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
        }
    }

    public override int CalculateSpellResistChance(GameLiving target)
    {
        return 25;
    }
}