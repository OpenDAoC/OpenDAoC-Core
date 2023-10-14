namespace DOL.GS.Spells
{
    /// <summary>
    /// They have less chance of landing melee attacks, and spells have a greater chance of affecting them.
    /// Note: Spell to hit code is located in CalculateToHitChance method in SpellHandler.cs
    /// </summary>
    [SpellHandler("HarpyFeatherCloak")]
    public class HarpyFeatherCloak : SingleStatDebuff
    {
        public HarpyFeatherCloak(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }

        /// <summary>
        /// This is melee to hit penalty
        /// </summary>
        public override EProperty Property1
        {
            get { return EProperty.ToHitBonus; }
        }
    }
}