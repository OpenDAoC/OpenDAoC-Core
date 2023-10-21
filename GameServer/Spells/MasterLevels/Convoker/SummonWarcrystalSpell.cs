using Core.Database;
using Core.Database.Tables;

namespace Core.GS.Spells
{
    //shared timer 1

    [SpellHandler("SummonWarcrystal")]
    public class SummonWarcrystalSpell : SummonItemSpellHandler
    {
        public SummonWarcrystalSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            string ammo = "";
            switch (Util.Random(1, 2))
            {
                case 1:
                    ammo = "mystic_ammo_heat";
                    break;
                case 2:
                    ammo = "mystic_ammo_cold";
                    break;
            }

            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(ammo);
            if (template != null)
            {
                items.Add(GameInventoryItem.Create(template));
                foreach (DbInventoryItem item in items)
                {
                    if (item.IsStackable)
                    {
                        item.Count = 1;
                        item.Weight = item.Count * item.Weight;
                    }
                }

            }
        }
    }
}