namespace DOL.GS
{
    public enum eHealthChangeType : byte
    {
        /// <summary>
        /// The health was changed by something unknown
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Regeneration changed the health
        /// </summary>
        Regenerate = 1,
        /// <summary>
        /// A spell changed the health
        /// </summary>
        Spell = 2,
        /// <summary>
        /// A potion changed the health
        /// </summary>
        Potion = 3
    }
}