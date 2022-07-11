using DOL.GS.PacketHandler;
using DOL.GS.ServerRules;
using System;
namespace DOL.GS.Commands
{
    [Cmd("&dfowner", ePrivLevel.Admin,
        "Changes the Realm owning access to Darkness Falls", "&dfowner <Realm>")]

    public class DFOwnerCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length == 2)
            {
                try
                {
                    byte newRealm = Convert.ToByte(args[1]);
                    var player = client.Player;

                    if (newRealm < 0 || newRealm > 3)
                    {
                        if (client != null && player != null) 
                            client.Out.SendMessage(player.Name + "'s realm can only be set to numbers 0-3!", eChatType.CT_Important,
                                eChatLoc.CL_SystemWindow);
                        return;
                    }
                    DFEnterJumpPoint.SetDFOwner(player, (eRealm)newRealm);

                }

                catch (Exception)
                {
                    DisplaySyntax(client);
                }
            }
            else
                DisplaySyntax(client);
        }

    }
}