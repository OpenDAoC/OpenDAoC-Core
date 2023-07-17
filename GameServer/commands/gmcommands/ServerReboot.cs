﻿ //Created by Loki2020


using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Commands
{
    [Cmd("&ServerReboot",ePrivLevel.GM,"Restarts the server instantly!!")]
    public class RestartCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public void OnCommand(GameClient client, string[] args)
        {
            client.Out.SendCustomDialog(string.Format("Do you wish to reboot the server instantly!!"), new CustomDialogResponse(RebootResponse));
        }
		protected void RebootResponse(GamePlayer player, byte response)
		{		
			if (response != 0x01)
			{			
				return;
			}

            new Thread(new ThreadStart(ShutDownServer)).Start();
            log.Info("Server Rebooted by " + player.Name + "");           
        }
        public static void ShutDownServer()
        {
            if (GameServer.Instance.IsRunning)
            {
                GameServer.Instance.Stop();  
                Thread.Sleep(2000);
                Process.Start(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "CoreServer.exe"));
                Environment.Exit(0);
            }
        }
    }
}
