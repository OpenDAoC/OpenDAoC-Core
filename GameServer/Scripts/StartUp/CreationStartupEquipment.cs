using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Commands;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.ServerProperties;
using log4net;

namespace Core.GS.GameEvents
{
	/// <summary>
	/// Give some Default Startup Equipment to newly created Character based on StarterEquipment Table.
	/// </summary>
	public static class CreationStartupEquipment
	{
		#region Properties
		/// <summary>
		/// Enable the Free Starter Equipment Gift.
		/// </summary>
		[Properties("startup", "enable_free_starter_equipment", "Enable Startup Free Equipment gifts imported from StarterEquipment Table", true)]
		public static bool ENABLE_FREE_STARTER_EQUIPMENT;
		#endregion

		/// <summary>
		/// Declare a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		/// <summary>
		/// Table Cache
		/// </summary>
		private static readonly Dictionary<EPlayerClass, List<DbItemTemplate>> m_cachedClassEquipment = new Dictionary<EPlayerClass, List<DbItemTemplate>>();
		
		/// <summary>
		/// Register Character Creation Events
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[ScriptLoadedEvent]
		public static void OnScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			InitStarterEquipment();
			GameEventMgr.AddHandler(DatabaseEvent.CharacterCreated, new CoreEventHandler(OnCharacterCreation));
		}
		
		/// <summary>
		/// Init (Or Refresh) Starter Equipment Cache
		/// </summary>
		[RefreshCommand]
		public static void InitStarterEquipment()
		{
			m_cachedClassEquipment.Clear();
			
			// Init Startup Collection.
			foreach (var equipclass in GameServer.Database.SelectAllObjects<StarterEquipment>())
			{
				if (equipclass.Template != null)
				{
					foreach(var classID in Util.SplitCSV(equipclass.Class, true))
					{
						int cId;
						if (int.TryParse(classID, out cId))
						{
							try
							{
								EPlayerClass gameClass = (EPlayerClass)cId;
								if (!m_cachedClassEquipment.ContainsKey(gameClass))
									m_cachedClassEquipment.Add(gameClass, new List<DbItemTemplate>());
								
								m_cachedClassEquipment[gameClass].Add(equipclass.Template);
							}
							catch (Exception e)
							{
								if (log.IsWarnEnabled)
									log.WarnFormat("Could not Add Starter Equipement for Record - ID: {0}, ClassID(s): {1}, Itemtemplate: {2}, while parsing {3}\n{4}",
									               equipclass.StarterEquipmentID, equipclass.Class, equipclass.TemplateID, classID, e);
							}
						}
					}
				}
				else
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("Cannot Find Item Template for Record - ID: {0}, ClassID(s): {1}, Itemtemplate: {2}", equipclass.StarterEquipmentID, equipclass.Class, equipclass.TemplateID);
				}
			}
		}
		
		/// <summary>
		/// Unregister Character Creation Events
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(DatabaseEvent.CharacterCreated, new CoreEventHandler(OnCharacterCreation));
		}
		
		/// <summary>
		/// On Character Creation set up equipment from StarterEquipment Table.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void OnCharacterCreation(CoreEvent e, object sender, EventArgs args)
		{
			if (!ENABLE_FREE_STARTER_EQUIPMENT)
				return;
			
			// Check Args
			var chArgs = args as CharacterEventArgs;
			
			if (chArgs == null)
				return;
			
			DbCoreCharacter ch = chArgs.Character;
			
			try
			{
				var usedSlots = new Dictionary<EInventorySlot, bool>();
				
				if (m_cachedClassEquipment.ContainsKey((EPlayerClass)ch.Class))
				{
					// sort for filling righ hand first...
					foreach (var item in m_cachedClassEquipment.Where(k => k.Key == 0 || k.Key == (EPlayerClass)ch.Class).SelectMany(kv => kv.Value).OrderBy(it => it.Item_Type))
					{
						// create Inventory item and set to owner.
						DbInventoryItem inventoryItem = GameInventoryItem.Create(item);
						inventoryItem.OwnerID = ch.ObjectId;
						inventoryItem.Realm = ch.Realm;
						
						bool itemChoosen = false;
		
						// if equipable item, find equippable slot
						foreach (EInventorySlot currentSlot in GameLivingInventory.EQUIP_SLOTS)
						{
							if ((EInventorySlot)inventoryItem.Item_Type == currentSlot)
							{
								EInventorySlot chosenSlot;
		
								// try to set Left Hand in Right Hand slot if not already used.
								if (currentSlot == EInventorySlot.LeftHandWeapon && (EObjectType)inventoryItem.Object_Type != EObjectType.Shield && !usedSlots.ContainsKey(EInventorySlot.RightHandWeapon))
								{
									chosenSlot = EInventorySlot.RightHandWeapon;
								}
								else
								{
									chosenSlot = currentSlot;
								}
		
								// Slot is occupied, add this to backpack.
								if (usedSlots.ContainsKey(chosenSlot))
								{
									if (log.IsWarnEnabled)
										log.WarnFormat("Cannot add Starter Equipment item {0} to class {1} an item is already assigned to this slot! (Added to Backpack...)", item.Id_nb, ch.Class);
									break;
								}
		
								inventoryItem.SlotPosition = (int)chosenSlot;
								usedSlots[chosenSlot] = true;
								if (ch.ActiveWeaponSlot == 0)
								{
									switch (inventoryItem.SlotPosition)
									{
										case Slot.RIGHTHAND:
											ch.ActiveWeaponSlot = (byte)EActiveWeaponSlot.Standard;
											break;
										case Slot.TWOHAND:
											ch.ActiveWeaponSlot = (byte)EActiveWeaponSlot.TwoHanded;
											break;
										case Slot.RANGED:
											ch.ActiveWeaponSlot = (byte)EActiveWeaponSlot.Distance;
											break;
									}
									
									// Save char to DB if Active Slot changed...
									if (ch.ActiveWeaponSlot != 0)
										GameServer.Database.SaveObject(ch);
								}
								
								itemChoosen = true;
								break;
							}
						}
						
						if (!itemChoosen)
						{
							//otherwise stick the item in the backpack
							for (int i = (int)EInventorySlot.FirstBackpack; i < (int)EInventorySlot.LastBackpack; i++)
							{
								if (!usedSlots.ContainsKey((EInventorySlot)i))
								{
									inventoryItem.SlotPosition = i;
									usedSlots[(EInventorySlot)i] = true;
									break;
								}
							}
						}
						
						GameServer.Database.AddObject(inventoryItem);
					}
				}
				
			}
			catch (Exception err)
			{
				if (log.IsErrorEnabled)
					log.ErrorFormat("Error while adding Startup Equipment to {0} - Exception: {1}", ch.Name, err);
			}
		}
	}
}
