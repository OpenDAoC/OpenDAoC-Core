namespace DOL.GS
{
    public class BdBufferSubPet : BdSubPet
    {
        public BdBufferSubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        public override void InitializeActiveWeaponFromInventory()
        {
            MinionGetWeapon(CommanderPet.eWeaponType.Staff);
        }
    }
}
