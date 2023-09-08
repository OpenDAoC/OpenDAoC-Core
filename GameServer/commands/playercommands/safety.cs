namespace DOL.GS.Commands
{
    [CmdAttribute(
         "&safety",
         ePrivLevel.Player,
         "Turns off PvP safety flag.",
         "/safety off")]
    public class SafetyCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if(args.Length >= 2 && args[1].ToLower() == "off")
            {
                client.Player.SafetyFlag = false;
                DisplayMessage(client, "Your safety flag is now set to OFF!  You can now attack non allied players, as well as be attacked.");
            }
            else if(client.Player.SafetyFlag)
            {
                DisplayMessage(client, "The safety flag keeps your character from participating in combat");
                DisplayMessage(client, "with non allied players in designated zones when you are below level 10.");
                DisplayMessage(client, "Type /safety off to begin participating in PvP combat in these zones, though once it is off it can NOT be turned back on!");
            }
            else
            {
                DisplayMessage(client, "Your safety flag is already off.");
            }
        }
    }
}
