using Core.Database.Tables;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

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