using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Battlemaster

    [SpellHandler("MLEndudrain")]
    public class SappingStrikeSpell : MasterLevelSpellHandling
    {
        public SappingStrikeSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }


        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }


        public override void OnDirectEffect(GameLiving target)
        {
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

            target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
        }
    }
}