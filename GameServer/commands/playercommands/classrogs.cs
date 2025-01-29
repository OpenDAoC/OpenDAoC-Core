namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&classrog",
        ePrivLevel.Player,
        "change the chance% of getting ROGs outside of your current class at level 50," +
        " or the likelihood of getting items relevant to your spec while under 50",
        "/classrog <%chance>")]
    public class ClassRogsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            int ROGCap = 0;
            int cachedInput = 0;

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
}