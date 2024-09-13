using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Spell handler to summon a animist pet.
    /// </summary>
    /// <author>IST</author>
    [SpellHandler(eSpellType.SummonAnimistPet)]
    public class SummonAnimistMainPet : SummonAnimistPet
    {
        public SummonAnimistMainPet(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckEndCast(GameLiving selectedTarget)
        {
            if (Caster is GamePlayer && Caster.ControlledBrain != null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistPet.CheckBeginCast.AlreadyHaveaPet"), eChatType.CT_SpellResisted);
                return false;
            }

            return base.CheckEndCast(selectedTarget);
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template)
        {
            if (Spell.DamageType == 0)
                return new TurretMainPetCaster(template);

            if (Spell.DamageType == (eDamageType) 1)
                return new TurretMainPetTank(template);

            return base.GetGamePet(template);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            if (Spell.DamageType == 0)
                return new TurretMainPetCasterBrain(owner);

            if (Spell.DamageType == (eDamageType) 1)
                return new TurretMainPetTankBrain(owner);

            return base.GetPetBrain(owner);
        }
    }
}
