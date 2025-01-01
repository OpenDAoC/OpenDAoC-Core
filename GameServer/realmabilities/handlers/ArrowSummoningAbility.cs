using System;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
    public class ArrowSummoningAbility : TimedRealmAbility
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ArrowSummoningAbility(DbAbility dba, int level) : base(dba, level) { }
        public override void Execute(GameLiving living)
		{
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            GamePlayer player = living as GamePlayer;
            DbItemTemplate arrow_summoning_1 = GameServer.Database.FindObjectByKey<DbItemTemplate>("arrow_summoning1");
            DbItemTemplate arrow_summoning_2 = GameServer.Database.FindObjectByKey<DbItemTemplate>("arrow_summoning2");
            DbItemTemplate arrow_summoning_3 = GameServer.Database.FindObjectByKey<DbItemTemplate>("arrow_summoning3");

			// what are these used for? - tolakram
            WorldInventoryItem as1 = WorldInventoryItem.CreateFromTemplate(arrow_summoning_1);
            WorldInventoryItem as2 = WorldInventoryItem.CreateFromTemplate(arrow_summoning_2);
            WorldInventoryItem as3 = WorldInventoryItem.CreateFromTemplate(arrow_summoning_3);

            if(!player.Inventory.AddTemplate(GameInventoryItem.Create(arrow_summoning_1),10,eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				player.Out.SendMessage("You do not have enough inventory space to place this item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
			else if (!player.Inventory.AddTemplate(GameInventoryItem.Create(arrow_summoning_2), 10, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
                player.Out.SendMessage("You do not have enough inventory space to place this item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
			else if (!player.Inventory.AddTemplate(GameInventoryItem.Create(arrow_summoning_3), 10, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
                player.Out.SendMessage("You do not have enough inventory space to place this item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}			

			GameEventMgr.AddHandler(player,GamePlayerEvent.Quit, new DOLEventHandler(PlayerQuit));	
            DisableSkill(living);	
		}
        public override int GetReUseDelay(int level)
        {
            switch (level)
            {
                case 1: return 900;
                case 2: return 300;
                case 3: return 5;
            }
            return 600;
        }
		public void PlayerQuit(DOLEvent e, object sender, EventArgs arguments)
		{
			GamePlayer player = sender as GamePlayer;
			if (player == null) return;		
			lock (player.Inventory.Lock)
			{
                DbInventoryItem item = player.Inventory.GetFirstItemByID("arrow_summoning1", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
				while (item != null)
				{
					player.Inventory.RemoveItem(item);
                    item = player.Inventory.GetFirstItemByID("arrow_summoning1", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
				}
                item = player.Inventory.GetFirstItemByID("arrow_summoning2", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
				while (item != null)
				{
					player.Inventory.RemoveItem(item);
                    item = player.Inventory.GetFirstItemByID("arrow_summoning2", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
				}
                item = player.Inventory.GetFirstItemByID("arrow_summoning3", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
				while (item != null)
				{
					player.Inventory.RemoveItem(item);
                    item = player.Inventory.GetFirstItemByID("arrow_summoning3", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
				}
			}
		}
		[ScriptLoadedEvent]
		public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
            if (!ServerProperties.Properties.LOAD_ARROW_SUMMONING)
                return;            
            
            DbItemTemplate arrow_summoning1 = GameServer.Database.FindObjectByKey<DbItemTemplate>("arrow_summoning1");
			if (arrow_summoning1 == null)
			{
				arrow_summoning1 = new DbItemTemplate();
				arrow_summoning1.Name = "mystical barbed footed flight broadhead arrows";
				arrow_summoning1.Level = 1;
				arrow_summoning1.MaxDurability = 100;
				arrow_summoning1.MaxCondition = 50000;
				arrow_summoning1.Quality = 100;
				arrow_summoning1.DPS_AF = 0;
				arrow_summoning1.SPD_ABS = 47;
				arrow_summoning1.Hand = 0;
				arrow_summoning1.Type_Damage = 3;
				arrow_summoning1.Object_Type = 43;
				arrow_summoning1.Item_Type = 40;
				arrow_summoning1.Weight = 0;
				arrow_summoning1.Model = 1635;
				arrow_summoning1.IsPickable = true;
				arrow_summoning1.IsDropable = false;
				arrow_summoning1.IsTradable = false;
				arrow_summoning1.MaxCount = 20;
				arrow_summoning1.Id_nb = "arrow_summoning1";
				GameServer.Database.AddObject(arrow_summoning1);
				if (log.IsDebugEnabled)
					log.Debug("Added " + arrow_summoning1.Id_nb);
			}
			DbItemTemplate arrow_summoning2 = GameServer.Database.FindObjectByKey<DbItemTemplate>("arrow_summoning2");
			if (arrow_summoning2 == null)
			{
				arrow_summoning2 = new DbItemTemplate();
				arrow_summoning2.Name = "mystical keen footed flight broadhead arrows";
				arrow_summoning2.Level = 1;
				arrow_summoning2.MaxDurability = 100;
				arrow_summoning2.MaxCondition = 50000;
				arrow_summoning2.Quality = 100;
				arrow_summoning2.DPS_AF = 0;
				arrow_summoning2.SPD_ABS = 47;
				arrow_summoning2.Hand = 0;
				arrow_summoning2.Type_Damage = 3;
				arrow_summoning2.Object_Type = 43;
				arrow_summoning2.Item_Type = 40;
				arrow_summoning2.Weight = 0;
				arrow_summoning2.Model = 1635;
				arrow_summoning2.IsPickable = true;
				arrow_summoning2.IsDropable = false;
				arrow_summoning2.IsTradable = false;
				arrow_summoning2.MaxCount = 20;
				arrow_summoning2.Id_nb = "arrow_summoning2";
				GameServer.Database.AddObject(arrow_summoning2);
				if (log.IsDebugEnabled)
					log.Debug("Added " + arrow_summoning2.Id_nb);
			}
			DbItemTemplate arrow_summoning3 = GameServer.Database.FindObjectByKey<DbItemTemplate>("arrow_summoning3");
			if (arrow_summoning3 == null)
			{
				arrow_summoning3 = new DbItemTemplate();
				arrow_summoning3.Name = "mystical blunt footed flight broadhead arrows";
				arrow_summoning3.Level = 1;
				arrow_summoning3.MaxDurability = 100;
				arrow_summoning3.MaxCondition = 50000;
				arrow_summoning3.Quality = 100;
				arrow_summoning3.DPS_AF = 0;
				arrow_summoning3.SPD_ABS = 47;
				arrow_summoning3.Hand = 0;
				arrow_summoning3.Type_Damage = 3;
				arrow_summoning3.Object_Type = 43;
				arrow_summoning3.Item_Type = 40;
				arrow_summoning3.Weight = 0;
				arrow_summoning3.Model = 1635;
				arrow_summoning3.IsPickable = true;
				arrow_summoning3.IsDropable = false;
				arrow_summoning3.IsTradable = false;
				arrow_summoning3.MaxCount = 20;
				arrow_summoning3.Id_nb = "arrow_summoning3";
				GameServer.Database.AddObject(arrow_summoning3);
				if (log.IsDebugEnabled)
					log.Debug("Added " + arrow_summoning3.Id_nb);
			}
		}
	}
}
