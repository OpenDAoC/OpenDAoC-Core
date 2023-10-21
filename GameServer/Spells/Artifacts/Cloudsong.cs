namespace DOL.GS.Spells
{
    [SpellHandler("CloudsongAura")]
    public class CloudsongAuraSpellHandler : DualStatBuff
    {
        public CloudsongAuraSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }

        /// <summary>
        /// SpecBuffBonusCategory
        /// </summary>
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.SpecBuff; } }

        /// <summary>
        /// BaseBuffBonusCategory
        /// </summary>
		public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.BaseBuff; } }

        public override EProperty Property1
        {
            get { return EProperty.SpellRange; }
        }

        public override EProperty Property2
        {
            get { return EProperty.ResistPierce; }
        }

    }

    /// <summary>
    /// [Freya] Nidel : Handler for Fall damage reduction.
    /// Calcul located in PlayerPositionUpdateHandler.cs
    /// </summary>
    [SpellHandler("CloudsongFall")]
    public class CloudsongFallSpellHandler : SpellHandler
    {
        public CloudsongFallSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }

        public override bool HasPositiveEffect
        {
            get { return true; }
        }
    }
}
