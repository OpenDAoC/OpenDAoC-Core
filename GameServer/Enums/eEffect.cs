namespace DOL.GS
{
    public enum eEffect
    {
        
        #region Positive Effects
        //positive effects
        Bladeturn,
        DamageAdd,
        DamageReturn,
        FocusShield,
        AblativeArmor,
        MeleeDamageBuff,
        MeleeHasteBuff,
        Celerity,
        MovementSpeedBuff,
        HealOverTime,

        //stats
        BaseStr,
        BaseDex,
        BaseCon,
        StrCon,
        DexQui,
        Acuity,
        SpecAf,
        BaseAf,
        PaladinAf,
        ArmorAbsorptionBuff,

        //resists
        BodyResistBuff,
        SpiritResistBuff,
        EnergyResistBuff,
        HeatResistBuff,
        ColdResistBuff,
        MatterResistBuff,

        //regen
        HealthRegenBuff,
        EnduranceRegenBuff,
        PowerRegenBuff,

        #endregion

        #region Negative Effects
        //persistent negative effects
        DamageOverTime,
        Bleed,
        Charm,
        MovementSpeedDebuff,
        MeleeDamageDebuff,
        MeleeHasteDebuff,
        Disease,

        //Crowd Control Effects
        Stun,
        StunImmunity,
        Mez,
        MezImmunity,
        MeleeSnare,
        Snare,
        SnareImmunity,
        Nearsight,

        //stat debuffs
        StrengthDebuff,
        DexterityDebuff,
        ConstitutionDebuff,
        StrConDebuff, 
        DexQuiDebuff,
        AcuityDebuff,
        ArmorFactorDebuff,
        ArmorAbsorptionDebuff,

        //resist debuffs
        BodyResistDebuff,
        SpiritResistDebuff,
        EnergyResistDebuff,
        HeatResistDebuff,
        ColdResistDebuff,
        MatterResistDebuff,

        #endregion

        //other
        Unknown,
        DirectDamage,
        Pulse
    }
}