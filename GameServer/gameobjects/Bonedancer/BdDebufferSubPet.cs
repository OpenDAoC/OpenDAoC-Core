namespace DOL.GS
{
    public class BdDebufferSubPet : BdSubPet
    {
        public BdDebufferSubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        public override void InitializeActiveWeaponFromInventory()
        {
            MinionGetWeapon(CommanderPet.eWeaponType.OneHandHammer);
        }
    }
}
