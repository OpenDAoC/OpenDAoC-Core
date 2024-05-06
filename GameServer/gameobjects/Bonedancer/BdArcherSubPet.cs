namespace DOL.GS
{
    public class BdArcherSubPet : BdSubPet
    {
        public BdArcherSubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        public override void InitializeActiveWeaponFromInventory()
        {
            MinionGetWeapon(CommanderPet.eWeaponType.OneHandSword);
            MinionGetWeapon(CommanderPet.eWeaponType.Bow);
        }
    }
}
