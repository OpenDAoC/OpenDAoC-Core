using DOL.AI.Brain;
using DOL.Database;

namespace DOL.GS
{
    public class BdSubPet : BdPet
    {
        /// <summary>
        /// Holds the different subpet ids
        /// </summary>
        public enum SubPetType : byte
        {
            Melee = 0,
            Healer = 1,
            Caster = 2,
            Debuffer = 3,
            Buffer = 4,
            Archer = 5
        }

        /// <summary>
        /// Create a minion.
        /// </summary>
        /// <param name="npcTemplate"></param>
        /// <param name="owner"></param>
        public BdSubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        /// <summary>
        /// Changes the commander's weapon to the specified weapon template
        /// </summary>
        public void MinionGetWeapon(CommanderPet.eWeaponType weaponType)
        {
            DbItemTemplate itemTemp = CommanderPet.GetWeaponTemplate(weaponType);

            if (itemTemp == null)
                return;

            DbInventoryItem weapon = GameInventoryItem.Create(itemTemp);

            if (weapon != null)
            {
                if (Inventory == null)
                    Inventory = new GameNPCInventory(new GameNpcInventoryTemplate());
                else
                    Inventory.RemoveItem(Inventory.GetItem((eInventorySlot)weapon.Item_Type));

                Inventory.AddItem((eInventorySlot)weapon.Item_Type, weapon);
                SwitchWeapon((eActiveWeaponSlot)weapon.Hand);
            }
        }

        public override void Die(GameObject killer)
        {
            CommanderPet commander = (this.Brain as IControlledBrain).Owner as CommanderPet;
            commander.RemoveControlledBrain(this.Brain as IControlledBrain);
            base.Die(killer);
        }
    }
}
