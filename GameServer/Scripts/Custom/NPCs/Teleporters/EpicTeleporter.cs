using System;
using System.Reflection;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using log4net;

namespace Core.GS.Scripts
{
	public class EpicTeleporter: GameNpc
	{
		private static new readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            switch (Realm)
            {
                case ERealm.Albion:
                    template.AddNPCEquipment(EInventorySlot.TorsoArmor, 2230); break;
                case ERealm.Midgard:
                    template.AddNPCEquipment(EInventorySlot.TorsoArmor, 2232);
                    template.AddNPCEquipment(EInventorySlot.ArmsArmor, 2233);
                    template.AddNPCEquipment(EInventorySlot.LegsArmor, 2234);
                    template.AddNPCEquipment(EInventorySlot.HandsArmor, 2235);
                    template.AddNPCEquipment(EInventorySlot.FeetArmor, 2236);
                    break;
                case ERealm.Hibernia:
                    template.AddNPCEquipment(EInventorySlot.TorsoArmor, 2231); ; break;
            }

            Inventory = template.CloseTemplate();
            Flags |= ENpcFlags.PEACE;
            Name = "Celestius Teleporter";
            GuildName = "GM Only Please";
            Model = 342;
            Level = 75;
            return base.AddToWorld();
        }
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			//TurnTo(player.X,player.Y);
			player.Out.SendMessage("Hello "+player.Name+"! I can teleport you to [Celestius]", EChatType.CT_Say,EChatLoc.CL_PopupWindow);
			return true;
		}
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if(!base.WhisperReceive(source,str)) return false;
		  	if(!(source is GamePlayer)) return false;
			GamePlayer t = (GamePlayer) source;
			//TurnTo(t.X,t.Y);
			switch(str)
			{

                case "Celestius":

                    //if (t.Group.MemberCount >= 4) //You have enough
                    {
                        Say("I'm now teleporting you to the Celestius");
                        t.MoveTo(91, 31955, 30276, 15733, 35);
                        break;
                    }
                    //else if (t.Group.MemberCount <= 3) //You dont have enough
                        //t.Out.SendMessage("You need a group of at least 4 adventurers for this encounter!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
            

                default: break;
			}
			return true;
		}
		private void SendReply(GamePlayer target, string msg)
			{
				target.Client.Out.SendMessage(
					msg,
					EChatType.CT_Say,EChatLoc.CL_PopupWindow);
			}
		[ScriptLoadedEvent]
        public static void OnScriptCompiled(CoreEvent e, object sender, EventArgs args)
        {
            log.Info("\tTeleporter initialized: true");
        }	
    }
	
}