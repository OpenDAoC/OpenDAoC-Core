namespace DOL.GS.Commands;

[Command(
	"&speed",
	EPrivLevel.GM,
	"Change base speed of target (no parameter to see current speed)",
	"/speed [newSpeed]")]
public class SpeedCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
        GamePlayer player = client.Player;
        GameLiving target = player.TargetObject as GameLiving;

        if ( target == null )
        {
            DisplayMessage( client, "You have not selected a valid target" );
            return;
        }

		if (args.Length == 1)
		{
            DisplayMessage( player, ( player == target ? "Your" : target.Name ) + " maximum speed is " + target.MaxSpeedBase );
			return;
		}

        short speed;

        if ( short.TryParse( args[1], out speed ) )
        {
            target.MaxSpeedBase = speed;

            GameNpc npc = target as GameNpc;

            if ( npc == null )
            {
                GamePlayer targetPlayer = target as GamePlayer;

                if ( targetPlayer != null )
                {
                    targetPlayer.Out.SendUpdateMaxSpeed();
                }
            }
            else
            {
                if ( npc.LoadedFromScript == false )
                {
                    npc.SaveIntoDatabase();
                }
            }

            DisplayMessage( player, ( player == target ? "Your" : target.Name ) + " maximum speed is now " + target.MaxSpeedBase );
        }
        else
        {
            DisplaySyntax( client );
        }
    }
}