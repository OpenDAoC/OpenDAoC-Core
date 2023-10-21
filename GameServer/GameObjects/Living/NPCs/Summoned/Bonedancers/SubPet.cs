using Core.AI.Brain;
using Core.Database;
using Core.GS.ServerProperties;

namespace Core.GS;

public class SubPet : BonedancerPet
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public bool MinionsAssisting {
        get { return Owner is CommanderPet commander && commander.MinionsAssisting; }
    }

    protected string m_PetSpecLine = null;
    /// <summary>
    /// Returns the spell line specialization this pet was summoned from
    /// </summary>
    public string PetSpecLine {
        get {
            // This is really inefficient, so only do it once, and only if we actually need it
            if (m_PetSpecLine == null && Brain is IControlledBrain brain && brain.GetPlayerOwner() is GamePlayer player)
            {
                // Get the spell that summoned this pet
                DbSpell dbSummoningSpell = CoreDb<DbSpell>.SelectObject(DB.Column("LifeDrainReturn").IsEqualTo(NPCTemplate.TemplateId));
                if (dbSummoningSpell != null)
                {
                    // Figure out which spell line the summoning spell is from
                    DbLineXSpell dbLineSpell = CoreDb<DbLineXSpell>.SelectObject(DB.Column("SpellID").IsEqualTo(dbSummoningSpell.SpellID));
                    if (dbLineSpell != null)
                    {
                        // Now figure out what the spec name is
                        SpellLine line = player.GetSpellLine(dbLineSpell.LineName);
                        if (line != null)
                            m_PetSpecLine = line.Spec;
                    }
                }
            }

            return m_PetSpecLine;
        }
    }

    /// <summary>
    /// Create a minion.
    /// </summary>
    /// <param name="npcTemplate"></param>
    /// <param name="owner"></param>
    public SubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

    /// <summary>
    /// Changes the commander's weapon to the specified weapon template
    /// </summary>
    public void MinionGetWeapon(CommanderPet.eWeaponType weaponType)
    {
        DbItemTemplate itemTemp = CommanderPet.GetWeaponTemplate(weaponType);

        if (itemTemp == null)
            return;

        DbInventoryItem weapon;

        weapon = GameInventoryItem.Create(itemTemp);
        if (weapon != null)
        {
            if (Inventory == null)
                Inventory = new GameNpcInventory(new GameNpcInventoryTemplate());
            else
                Inventory.RemoveItem(Inventory.GetItem((EInventorySlot)weapon.Item_Type));

            Inventory.AddItem((EInventorySlot)weapon.Item_Type, weapon);
            SwitchWeapon((EActiveWeaponSlot)weapon.Hand);
        }
    }

    /// <summary>
    /// Sort spells into specific lists, scaling pet spell as appropriate
    /// </summary>
    public override void SortSpells()
    {
        if (Spells.Count < 1 || Level < 1)
            return;

        base.SortSpells();

        if (Properties.PET_SCALE_SPELL_MAX_LEVEL > 0)
        {
            int scaleLevel = Level;

            // Some minions have multiple spells, so only grab their owner's spec once per pet.
            if (Properties.PET_CAP_BD_MINION_SPELL_SCALING_BY_SPEC
                && Brain is IControlledBrain brain && brain.GetPlayerOwner() is GamePlayer BD)
            {
                int spec = BD.GetModifiedSpecLevel(PetSpecLine);

                if (spec > 0 && spec < scaleLevel)
                    scaleLevel = spec;
            }

            ScaleSpells(scaleLevel);
        }
    }

    /// <summary>
    /// Scale the passed spell according to PET_SCALE_SPELL_MAX_LEVEL, capping by BD spec if appropriate
    /// </summary>
    /// <param name="spell">The spell to scale</param>
    /// <param name="casterLevel">The level to scale the pet spell to, 0 to use pet level</param>
    public override void ScalePetSpell(Spell spell, int casterLevel = 0)
    {
        if (Properties.PET_SCALE_SPELL_MAX_LEVEL < 1 || spell == null || Level < 1)
            return;

        if (casterLevel < 1)
        {
            casterLevel = Level;

            // Style procs and subspells can't be scaled in advance, so we need to check BD spec here as well.
            if (Properties.PET_CAP_BD_MINION_SPELL_SCALING_BY_SPEC
                && Brain is IControlledBrain brain && brain.GetPlayerOwner() is GamePlayer BD)
            {
                int spec = BD.GetModifiedSpecLevel(PetSpecLine);

                if (spec > 0 && spec < casterLevel)
                    casterLevel = spec;
            }
        }

        //base.ScalePetSpell(spell, casterLevel);
    }

    public override void Die(GameObject killer)
    {
        CommanderPet commander = (this.Brain as IControlledBrain).Owner as CommanderPet;
        commander.RemoveControlledNpc(this.Brain as IControlledBrain);

        base.Die(killer);
    }
}