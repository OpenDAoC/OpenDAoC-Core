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
        CombatHeal,

        //stats
        PaladinAf,
        StrengthBuff,
        DexterityBuff,
        ConstitutionBuff,
        QuicknessBuff,
        StrengthConBuff,
        DexQuickBuff,
        AcuityBuff,
        SpecAFBuff,
        BaseAFBuff,
        ArmorAbsorptionBuff,

        //resists
        BodyResistBuff,
        SpiritResistBuff,
        EnergyResistBuff,
        HeatResistBuff,
        ColdResistBuff,
        MatterResistBuff,
        SlashResistBuff,
        CrushResistBuff,
        ThrustResistBuff,
        BodySpiritEnergyBuff,
        HeatColdMatterBuff,
        AllMagicResistsBuff,
        AllMeleeResistsBuff,
        AllResistsBuff,

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
        Confusion,
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
        NearsightImmunity,

        //stat debuffs
        StrengthDebuff,
        DexterityDebuff,
        ConstitutionDebuff,
        QuicknessDebuff,
        StrConDebuff,
        WsConDebuff,
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
        SlashResistDebuff,
        CrushResistDebuff,
        ThrustResistDebuff,
        NaturalResistDebuff,
        AllMeleeResistsDebuff,

        #endregion

        //other
        Unknown,
        DirectDamage,
        Pulse,
        FacilitatePainworking,
        MesmerizeDurationBuff,
        FatigueConsumptionBuff,
        SavageBuff,
        Pet,
        OffensiveProc,
        DefensiveProc,
        HereticPiercingMagic,
        PiercingMagic,
        ResurrectionIllness,
        RvrResurrectionIllness,
        Stealth,
        Sprint,
        Berserk,
        Engage,
        Guard,
        Intercept,
        Protect,
        QuickCast,
        RapidFire,
        SureShot,
        TrueShot,
        Stag,
        TripleWield,
        Camouflage,
        Shade,
        DirtyTricks,
        DirtyTricksDetrimental,
        Charge,
        NPCStunImmunity,
        NPCMezImmunity,
        // Realm Abilities
        BunkerOfFaith,
        AmelioratingMelodies,
        SoldiersBarricade,
        ReflexAttack,
        WhirlingDervish,
        Viper,
        MajesticWill,
        TrueSight,
        MasteryOfConcentration,
    }
}