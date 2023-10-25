using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Core.GS.Enums;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using log4net;

namespace Core.GS.Commands;

[Command("&ServerReboot",EPrivLevel.GM,"Restarts the server instantly!!")]
public class ServerRebootCommand : ACommandHandler, ICommandHandler
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
            Process.Start(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "DOLServer.exe"));
            Environment.Exit(0);
        }
    }
}