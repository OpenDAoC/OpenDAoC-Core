using Core.AI.Brain;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.GS.Spells;

namespace Core.spells
{
    /// <summary>
    /// Return life to Player Owner
    /// </summary>
    [SpellHandler("PetLifedrain")]
    public class PetLifedrainSpell : LifedrainSpell
    {
        public PetLifedrainSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void OnDirectEffect(GameLiving target)
        {
            if(Caster == null || !(Caster is GameSummonedPet) || !(((GameSummonedPet) Caster).Brain is IControlledBrain))
                return;
            base.OnDirectEffect(target);
        }

        public override void StealLife(AttackData ad)
        {
            if(ad == null) return;
            GamePlayer player = ((IControlledBrain) ((GameSummonedPet) Caster).Brain).GetPlayerOwner();
            if(player == null || !player.IsAlive) return;
            int heal = ((ad.Damage + ad.CriticalDamage)*m_spell.LifeDrainReturn)/100;
            if(player.IsDiseased)
            {
                MessageToLiving(player, "You are diseased !", EChatType.CT_SpellResisted);
                heal >>= 1;
            }
            if(heal <= 0) return;

            heal = player.ChangeHealth(player, EHealthChangeType.Spell, heal);
            if(heal > 0)
            {
                MessageToLiving(player, "You steal " + heal + " hit point" + (heal == 1 ? "." :"s."), EChatType.CT_Spell);
            } else
            {
                MessageToLiving(player, "You cannot absorb any more life.", EChatType.CT_SpellResisted);
            }
        }
    }
}
