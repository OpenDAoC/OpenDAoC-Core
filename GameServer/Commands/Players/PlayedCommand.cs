using System;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
	"&played",
	EPrivLevel.Player,
	"Returns the age of the character",
	"/played")]
public class PlayedCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "played"))
			return;
        if (args.Length > 1)
        {
            if (args[1].Equals("level"))
            {
                //int yearsPlayed = 0;
                //int monthsPlayed = 0;
                TimeSpan showPlayed = TimeSpan.FromSeconds(client.Player.PlayedTimeSinceLevel);
                int daysPlayed = showPlayed.Days;
                // Figure Years
                //if (showPlayed.Days >= 365)
                //{
                //    yearsPlayed = daysPlayed / 365;
                //    daysPlayed -= yearsPlayed * 365;
                //}
                // Figure Months (roughly)
                //if (showPlayed.Days >= 30)
                //{
                //    monthsPlayed = daysPlayed / 30;
                //    daysPlayed -= monthsPlayed * 30;
                //}

                client.Out.SendMessage("You have played for " /*+ yearsPlayed + " Years, " + monthsPlayed + " Months, "*/ + daysPlayed + " Days, " + showPlayed.Hours + " Hours and " + showPlayed.Minutes + " Minutes this level.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            }
        }
        else
        {

            //int yearsPlayed = 0;
            //int monthsPlayed = 0;
            TimeSpan showPlayed = TimeSpan.FromSeconds(client.Player.PlayedTime);
            int daysPlayed = showPlayed.Days;
            //// Figure Years
            //if (showPlayed.Days >= 365)
            //{
            //    yearsPlayed = daysPlayed / 365;
            //    daysPlayed -= yearsPlayed * 365;
            //}
            //// Figure Months (roughly)
            //if (showPlayed.Days >= 30)
            //{
            //    monthsPlayed = daysPlayed / 30;
            //    daysPlayed -= monthsPlayed * 30;
            //}

            client.Out.SendMessage("You have played for " /*+ yearsPlayed + " Years, " + monthsPlayed + " Months, "*/ + daysPlayed + " Days, " + showPlayed.Hours + " Hours and " + showPlayed.Minutes + " Minutes.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
        }
	}
}