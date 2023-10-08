using System;
using DOL.Database;

namespace DOL.GS.Commands;

[Command(
    "&classrog",
    EPrivLevel.Player,
    "change the chance% of getting ROGs outside of your current class at level 50," +
    " or the likelihood of getting items relevant to your spec while under 50",
    "/classrog <%chance>")]
public class ClassRogCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        
        DbAccountXRealmLoyalty realmLoyalty = CoreDb<DbAccountXRealmLoyalty>.SelectObject(DB.Column("AccountID").IsEqualTo(client.Account.ObjectId).And(DB.Column("Realm").IsEqualTo(client.Player.Realm)));
        int ROGCap = 0;
        int tmpLoyal = realmLoyalty.LoyalDays > 30 ? 30 : realmLoyalty.LoyalDays;
        int cachedInput = 0;
        if (realmLoyalty != null)
        {
            //max cap of 50% out of class chance
            //scaled by loyalty%
            ROGCap = (int)Math.Round(50 * (tmpLoyal / 30.0)); 
        }
        
        if (args.Length < 2)
        {
            DisplaySyntax(client);
            DisplayMessage(client, "Current cap: " + ROGCap);
            return;
        }

        cachedInput = int.Parse(args[1]);
        if ( cachedInput > ROGCap)
        {
            DisplayMessage(client, "Input too high. Defaulting to cap: " + ROGCap);
            cachedInput = ROGCap;
        }
        else if (cachedInput < 0)
        {
            DisplayMessage(client, "Input must be 0 or above. Current cap: " + ROGCap);
            return;
        }

        client.Player.OutOfClassROGPercent = cachedInput;
        
        if(client.Player.Level == 50)
            DisplayMessage(client, "You will now receive out of class ROGs " + client.Player.OutOfClassROGPercent + "% of the time.");
        else
        {
            //DisplayMessage(client, "You are now " + client.Player.OutOfClassROGPercent + "% more likely to get ROGs relevant to your spec.");
        }
    }
}