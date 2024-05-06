namespace DOL.GS
{
    public class BdHealerSubPet : BdSubPet
    {
        public BdHealerSubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        public override void InitializeActiveWeaponFromInventory()
        {
            MinionGetWeapon(CommanderPet.eWeaponType.Staff);
        }
    }
}
