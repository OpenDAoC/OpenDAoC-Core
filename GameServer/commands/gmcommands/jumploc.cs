using System;
using DOL.GS.PacketHandler;
using DOL.GS; 
using DOL.Language; 
using DOL.GS.Keeps; 

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&jumploc",
		ePrivLevel.Admin, 
		// Description updated to English
		"Teleports your character to the local x, y coordinates within the current zone (using Mythic zone coordinates 0-65535). Z-coordinate is optional.",
		// Command signature
		"/jumploc [x] [y] [z]")]
	public class JumplocCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			GamePlayer player = client.Player;

			if (IsSpammingCommand(client.Player, "jumploc"))
				return;

			// The command requires at least 2 arguments (command + x + y)
			if (args.Length < 3)
			{
				client.Out.SendMessage
					(
					// Message 1: Usage (English)
					"Usage: /jumploc <local_x> <local_y> [z]. Please enter at least X and Y coordinates.",
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
					);
				return;
			}
            
			// 1. Parameter Parsing (extended for optional Z-coordinate)
			int x_lokal = 0;
			int y_lokal = 0;
			int z_global = 0;
            
			try
			{
				x_lokal = System.Convert.ToInt32(args[1]);
				y_lokal = System.Convert.ToInt32(args[2]);
                
                // CHECK: If 4 arguments are present (command + x + y + z)
				if (args.Length >= 4)
				{
					z_global = System.Convert.ToInt32(args[3]);
				}
				else
				{
					// If Z is not specified, use the player's current Z coordinate
					z_global = player.Z; 
                    // Message 2: Z not specified (English)
                    player.Out.SendMessage("Z-coordinate not specified. Using current Z-coordinate.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
			catch 
			{
				// Message 3: Invalid input (English)
				client.Out.SendMessage("Please enter valid numeric values for X, Y, and Z (if specified).", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

            
            // 2. Retrieve Zone Offset
            if (player.CurrentZone == null)
            {
                // Message 4: Zone error (English)
                player.Out.SendMessage("Error: Could not determine current zone.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            
            int Xoffset = player.CurrentZone.XOffset;
			int Yoffset = player.CurrentZone.YOffset;

            // Validation of local coordinates (Mythic range)
            if (x_lokal < 0 || x_lokal > 65535 || y_lokal < 0 || y_lokal > 65535)
            {
                // Message 5: Validation error (English)
                player.Out.SendMessage($"Local x/y values must be between 0 and 65535. Received: ({x_lokal}, {y_lokal})", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // 3. Conversion to global coordinates
            int x_global = Xoffset + x_lokal;
            int y_global = Yoffset + y_lokal;

            // 4. Jump
            ushort regionID = player.CurrentRegionID; 
            
            player.MoveTo(regionID, x_global, y_global, z_global, player.Heading);

            // Message 6: Success message (English)
            player.Out.SendMessage(
                $"Jumping to local coordinates: ({x_lokal}, {y_lokal}, {z_global}) " +
                $"in Region {regionID} (Global: {x_global}, {y_global}, {z_global}).", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}
	}
}