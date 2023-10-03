namespace DOL.GS
{
    /// <summary>
    /// Holds the possible activeWeaponSlot values
    /// </summary>
    public enum eActiveWeaponSlot : byte
    {
        /// <summary>
        /// Weapon slot righthand
        /// </summary>
        Standard = 0x00,
        /// <summary>
        /// Weaponslot twohanded
        /// </summary>
        TwoHanded = 0x01,
        /// <summary>
        /// Weaponslot distance
        /// </summary>
        Distance = 0x02
    }
}
