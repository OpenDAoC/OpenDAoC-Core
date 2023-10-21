using Core.Database;

namespace Core.GS.Spells
{
    [SpellHandler("EssenceFlare")]
    public class EssenceFlareSpell : SummonItemSpellHandler
    {
        public EssenceFlareSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>("Meschgift");
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