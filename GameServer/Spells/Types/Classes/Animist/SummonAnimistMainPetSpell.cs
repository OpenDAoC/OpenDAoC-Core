using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Skills;

namespace Core.GS.Spells;

/// <summary>
/// Spell handler to summon a animist pet.
/// </summary>
/// <author>IST</author>
[SpellHandler("SummonAnimistPet")]
public class SummonAnimistMainPetSpell : SummonAnimistPetSpell
{
  public SummonAnimistMainPetSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
  {
  }

  public override bool CheckEndCast(GameLiving selectedTarget)
  {
    if(Caster is GamePlayer && Caster.ControlledBrain != null)
    {
      MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistPet.CheckBeginCast.AlreadyHaveaPet"), EChatType.CT_SpellResisted);
      return false;
    }
    return base.CheckEndCast(selectedTarget);
  }

  protected override IControlledBrain GetPetBrain(GameLiving owner)
  {
    if(Spell.DamageType == 0)
    {
      return new TurretMainPetCasterBrain(owner);
    }
    //[Ganrod] Nidel: Spell.DamageType : 1 for tank pet
    if(Spell.DamageType == (EDamageType) 1)
    {
      return new TurretMainPetTankBrain(owner);
    }
    return base.GetPetBrain(owner);
  }
}