namespace DOL.GS
{
    public class BdMeleeSubPet : BdSubPet
    {
        public BdMeleeSubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        public override void InitializeActiveWeaponFromInventory()
        {
            if (Util.Chance(50))
                MinionGetWeapon(CommanderPet.eWeaponType.TwoHandAxe);
            else
                MinionGetWeapon(CommanderPet.eWeaponType.OneHandAxe);
        }
    }
}
