using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.Expansions.TrialsOfAtlantis.Artifacts;

[SpellHandler("ShatterIllusions")]
public class ShatterIllusionsSpell : SpellHandler
{
    //Shatter Illusions 
    //(returns the enemy from their shapeshift forms 
    //causing 200 body damage to the enemy. Range: 1500) 
    public override void OnDirectEffect(GameLiving target)
    {
        AttackData ad = CalculateDamageToTarget(target);
        base.OnDirectEffect(target);
        foreach (GameSpellEffect effect in target.EffectList.GetAllOfType(typeof(GameSpellEffect)))
        {
            if (effect.SpellHandler.Spell.SpellType.Equals("ShadesOfMist") ||
                effect.SpellHandler.Spell.SpellType.Equals("TraitorsDaggerProc") ||
                effect.SpellHandler.Spell.SpellType.Equals("DreamMorph") ||
                effect.SpellHandler.Spell.SpellType.Equals("DreamGroupMorph") ||
                effect.SpellHandler.Spell.SpellType.Equals("MaddeningScalars") ||
                effect.SpellHandler.Spell.SpellType.Equals("AtlantisTabletMorph") ||
                effect.SpellHandler.Spell.SpellType.Equals("AlvarusMorph"))
            {
                ad.Damage = (int)Spell.Damage;
                effect.Cancel(false);
                SendEffectAnimation(target, 0, false, 1);
                SendDamageMessages(ad);
                DamageTarget(ad);
                return;
            }
        }
    }
    public virtual void DamageTarget(AttackData ad)
    {
        ad.AttackResult = EAttackResult.HitUnstyled;
        ad.Target.OnAttackedByEnemy(ad);
        ad.Attacker.DealDamage(ad);
        foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
        }
    }
    public ShatterIllusionsSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}