using DOL.GS;

public class BandOfStarsMorphECSEffect : MorphECSEffect
{
    public BandOfStarsMorphECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = eEffect.Morph;
    }

    public override void OnStartEffect()
    {
        Owner.DebuffCategory[(int)eProperty.Dexterity] += (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Strength] += (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Constitution] += (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Acuity] += (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Piety] += (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Empathy] += (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Quickness] += (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Intelligence] += (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Charisma] += (int)SpellHandler.Spell.Value;   
        Owner.DebuffCategory[(int)eProperty.ArmorAbsorption] += (int)SpellHandler.Spell.Value; 
        Owner.DebuffCategory[(int)eProperty.MagicAbsorption] += (int)SpellHandler.Spell.Value; 
        base.OnStartEffect();
    }

    public override void OnStopEffect()
    {
        Owner.DebuffCategory[(int)eProperty.Dexterity] -= (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Strength] -= (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Constitution] -= (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Acuity] -= (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Piety] -= (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Empathy] -= (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Quickness] -= (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Intelligence] -= (int)SpellHandler.Spell.Value;
        Owner.DebuffCategory[(int)eProperty.Charisma] -= (int)SpellHandler.Spell.Value;        
        Owner.DebuffCategory[(int)eProperty.ArmorAbsorption] -= (int)SpellHandler.Spell.Value; 
        Owner.DebuffCategory[(int)eProperty.MagicAbsorption] -= (int)SpellHandler.Spell.Value; 
        base.OnStopEffect();
    }
}