namespace DOL.GS
{
    public class BdCasterSubPet : BdSubPet
    {
        public BdCasterSubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        public override void InitializeActiveWeaponFromInventory()
        {
            MinionGetWeapon(CommanderPet.eWeaponType.Staff);
        }
    }
}
