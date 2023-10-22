using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("BeltOfMoon")]
public class BeltOfMoon : SummonItemSpellHandler
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	private DbItemTemplate m_MoonMace;
    private DbItemTemplate m_MoonStaff;

    public BeltOfMoon(GameLiving caster, Spell spell, SpellLine line)
		: base(caster, spell, line)
    {
        if (caster.CurrentRegion.IsNightTime)
        {
            if (caster.Realm == ERealm.Albion)
            {
                m_MoonMace = GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_Mace") ?? Mace;
                items.Add(GameInventoryItem.Create(m_MoonMace));

                m_MoonStaff = GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_Staff") ?? Staff;
                items.Add(GameInventoryItem.Create(m_MoonStaff));
                return;
            }

            if (caster.Realm == ERealm.Midgard)
            {
                m_MoonMace = GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_MaceM") ?? MaceM;
                items.Add(GameInventoryItem.Create(m_MoonMace));

                m_MoonStaff = GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_Staff") ?? Staff;
                items.Add(GameInventoryItem.Create(m_MoonStaff));
                return;
            }

            if (caster.Realm == ERealm.Hibernia)
            {
                m_MoonMace = GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_MaceH") ?? MaceH;
                items.Add(GameInventoryItem.Create(m_MoonMace));

                m_MoonStaff = GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_Staff") ?? Staff;
                items.Add(GameInventoryItem.Create(m_MoonStaff));
                return;
            }
        }

        else
        {
            MessageToCaster("The powers of the Belt of Moon, can only be Summon under the Moon light!", EChatType.CT_SpellResisted);
            return;
        }
    }

    #region Moon Mace
    private DbItemTemplate Mace
	{
		get
		{
			m_MoonMace = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_Mace");
			if (m_MoonMace == null)
			{
				if (log.IsWarnEnabled) log.Warn("Could not find Moon_Mace, loading it ...");
				m_MoonMace = new DbItemTemplate();
                m_MoonMace.Id_nb = "Moon_Mace";
                m_MoonMace.Name = "Moon Mace";
				m_MoonMace.Level = 50;
				m_MoonMace.Durability = 50000;
				m_MoonMace.MaxDurability = 50000;
				m_MoonMace.Condition = 50000;
				m_MoonMace.MaxCondition = 50000;
				m_MoonMace.Quality = 100;
				m_MoonMace.DPS_AF = 150;
				m_MoonMace.SPD_ABS = 35;
				m_MoonMace.Type_Damage = 0;
				m_MoonMace.Object_Type = 2;
				m_MoonMace.Item_Type = 10;
				m_MoonMace.Color = 43;
				m_MoonMace.Effect = 45;
				m_MoonMace.Model = 647;
				m_MoonMace.Bonus1 = 24;
				m_MoonMace.Bonus2 = 3;
				m_MoonMace.Bonus3 = 10;
				m_MoonMace.Bonus4 = 4;
				m_MoonMace.Bonus1Type = 156;
				m_MoonMace.Bonus2Type = 163;
				m_MoonMace.Bonus3Type = 196;
				m_MoonMace.Bonus4Type = 191;
				m_MoonMace.IsPickable = false;
				m_MoonMace.IsDropable = false;
				m_MoonMace.CanDropAsLoot = false;
				m_MoonMace.IsTradable = false;
				m_MoonMace.MaxCount = 1;
				m_MoonMace.PackSize = 1;
                m_MoonMace.SpellID = 65511;
                m_MoonMace.Charges = 5;
                m_MoonMace.MaxCharges = 5;

			}
			return m_MoonMace;
		}
	}

	private DbItemTemplate MaceM
	{
        get
        {
            m_MoonMace = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_Mace");
            if (m_MoonMace == null)
            {
                if (log.IsWarnEnabled) log.Warn("Could not find Moon_Mace, loading it ...");
                m_MoonMace = new DbItemTemplate();
                m_MoonMace.Id_nb = "Moon_Mace";
                m_MoonMace.Name = "Moon Warhammer";
                m_MoonMace.Level = 50;
                m_MoonMace.Durability = 50000;
                m_MoonMace.MaxDurability = 50000;
                m_MoonMace.Condition = 50000;
                m_MoonMace.MaxCondition = 50000;
                m_MoonMace.Quality = 100;
                m_MoonMace.DPS_AF = 150;
                m_MoonMace.SPD_ABS = 35;
                m_MoonMace.Type_Damage = 0;
                m_MoonMace.Object_Type = 12;
                m_MoonMace.Item_Type = 10;
                m_MoonMace.Color = 43;
                m_MoonMace.Effect = 45;
                m_MoonMace.Model = 320;
                m_MoonMace.Bonus1 = 24;
                m_MoonMace.Bonus2 = 3;
                m_MoonMace.Bonus3 = 10;
                m_MoonMace.Bonus4 = 4;
                m_MoonMace.Bonus1Type = 156;
                m_MoonMace.Bonus2Type = 163;
                m_MoonMace.Bonus3Type = 196;
                m_MoonMace.Bonus4Type = 191;
                m_MoonMace.IsPickable = false;
                m_MoonMace.IsDropable = false;
                m_MoonMace.CanDropAsLoot = false;
                m_MoonMace.IsTradable = false;
                m_MoonMace.MaxCount = 1;
                m_MoonMace.PackSize = 1;
                m_MoonMace.SpellID = 65511;
                m_MoonMace.Charges = 5;
                m_MoonMace.MaxCharges = 5;

            }
            return m_MoonMace;
        }
	}

	private DbItemTemplate MaceH
	{
        get
        {
            m_MoonMace = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_Mace");
            if (m_MoonMace == null)
            {
                if (log.IsWarnEnabled) log.Warn("Could not find Moon_Mace, loading it ...");
                m_MoonMace = new DbItemTemplate();
                m_MoonMace.Id_nb = "Moon_Mace";
                m_MoonMace.Name = "Moon Hammer";
                m_MoonMace.Level = 50;
                m_MoonMace.Durability = 50000;
                m_MoonMace.MaxDurability = 50000;
                m_MoonMace.Condition = 50000;
                m_MoonMace.MaxCondition = 50000;
                m_MoonMace.Quality = 100;
                m_MoonMace.DPS_AF = 150;
                m_MoonMace.SPD_ABS = 35;
                m_MoonMace.Type_Damage = 0;
                m_MoonMace.Object_Type = 20;
                m_MoonMace.Item_Type = 10;
                m_MoonMace.Color = 43;
                m_MoonMace.Effect = 45;
                m_MoonMace.Model = 461;
                m_MoonMace.Bonus1 = 24;
                m_MoonMace.Bonus2 = 3;
                m_MoonMace.Bonus3 = 10;
                m_MoonMace.Bonus4 = 4;
                m_MoonMace.Bonus1Type = 156;
                m_MoonMace.Bonus2Type = 163;
                m_MoonMace.Bonus3Type = 196;
                m_MoonMace.Bonus4Type = 191;
                m_MoonMace.IsPickable = false;
                m_MoonMace.IsDropable = false;
                m_MoonMace.CanDropAsLoot = false;
                m_MoonMace.IsTradable = false;
                m_MoonMace.MaxCount = 1;
                m_MoonMace.PackSize = 1;
                m_MoonMace.SpellID = 65511;
                m_MoonMace.Charges = 5;
                m_MoonMace.MaxCharges = 5;

            }
            return m_MoonMace;
        }
	}
    #endregion End of Moon Mace

    #region Moon Staff
    private DbItemTemplate Staff
    {
        get
        {
            m_MoonStaff = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Moon_Staff");
            if (m_MoonStaff == null)
            {
                if (log.IsWarnEnabled) log.Warn("Could not find Moon_Staff, loading it ...");
                m_MoonStaff = new DbItemTemplate();
                m_MoonStaff.Id_nb = "Moon_Staff";
                m_MoonStaff.Name = "Moon Staff";
                m_MoonStaff.Level = 50;
                m_MoonStaff.Durability = 50000;
                m_MoonStaff.MaxDurability = 50000;
                m_MoonStaff.Condition = 50000;
                m_MoonStaff.MaxCondition = 50000;
                m_MoonStaff.Quality = 100;
                m_MoonStaff.DPS_AF = 150;
                m_MoonStaff.SPD_ABS = 35;
                m_MoonStaff.Type_Damage = 0;
                m_MoonStaff.Object_Type = 8;
                m_MoonStaff.Item_Type = 12;
                m_MoonStaff.Hand = 1;
                m_MoonStaff.Color = 43;
                m_MoonStaff.Effect = 45;
                m_MoonStaff.Model = 566;
                m_MoonStaff.Bonus1 = 24;
                m_MoonStaff.Bonus2 = 3;
                m_MoonStaff.Bonus3 = 10;
                m_MoonStaff.Bonus4 = 4;
                m_MoonStaff.Bonus1Type = 156;
                m_MoonStaff.Bonus2Type = 163;
                m_MoonStaff.Bonus3Type = 196;
                m_MoonStaff.Bonus4Type = 191;
                m_MoonStaff.IsPickable = false;
                m_MoonStaff.IsDropable = false;
                m_MoonStaff.CanDropAsLoot = false;
                m_MoonStaff.IsTradable = false;
                m_MoonStaff.MaxCount = 1;
                m_MoonStaff.SpellID = 65511;
                m_MoonStaff.Charges = 5;
                m_MoonStaff.MaxCharges = 5;

            }
            return m_MoonStaff;
        }
    }
    #endregion End of Moon Staff


    public override void OnDirectEffect(GameLiving target)
	{
		base.OnDirectEffect(target);
        GameEventMgr.AddHandler(Caster, GamePlayerEvent.Released, OnPlayerReleased);
		GameEventMgr.AddHandler(Caster, GamePlayerEvent.Quit, OnPlayerLeft);
	}


    private static void OnPlayerReleased(CoreEvent e, object sender, EventArgs arguments)
    {
        if (!(sender is GamePlayer))
            return;

        GamePlayer player = sender as GamePlayer;

		lock (player.Inventory)
		{
            var items = player.Inventory.GetItemRange(EInventorySlot.MinEquipable, EInventorySlot.LastBackpack);
			foreach (DbInventoryItem invItem in items)
			{
                if (player.CurrentRegion.IsNightTime)
                {
                    return;
                }

                if (invItem.Id_nb.Equals("Moon_Mace"))
                    player.Inventory.RemoveItem(invItem);

                if (invItem.Id_nb.Equals("Moon_MaceM"))
                    player.Inventory.RemoveItem(invItem);

                if (invItem.Id_nb.Equals("Moon_MaceH"))
                    player.Inventory.RemoveItem(invItem);

                if (invItem.Id_nb.Equals("Moon_Staff"))
                    player.Inventory.RemoveItem(invItem);
                
                player.Out.SendMessage("The Power of Belt of Moon, has left you!",EChatType.CT_System, EChatLoc.CL_SystemWindow);
            }
		}
        GameEventMgr.RemoveHandler(sender, GamePlayerEvent.Released, OnPlayerReleased);
	}
    

	private static void OnPlayerLeft(CoreEvent e, object sender, EventArgs arguments)
	{
		if (!(sender is GamePlayer))
			return;

		GamePlayer player = sender as GamePlayer;
		lock (player.Inventory)
		{
            var items = player.Inventory.GetItemRange(EInventorySlot.MinEquipable, EInventorySlot.LastBackpack);
			foreach (DbInventoryItem invItem in items)
			{
                if (invItem.Id_nb.Equals("Moon_Mace"))
					player.Inventory.RemoveItem(invItem);

                if (invItem.Id_nb.Equals("Moon_MaceM"))
					player.Inventory.RemoveItem(invItem);

                if (invItem.Id_nb.Equals("Moon_MaceH"))
					player.Inventory.RemoveItem(invItem);

                if (invItem.Id_nb.Equals("Moon_Staff"))
                    player.Inventory.RemoveItem(invItem);

			}
		}
		GameEventMgr.RemoveHandler(sender, GamePlayerEvent.Quit, OnPlayerLeft);
   	}
}