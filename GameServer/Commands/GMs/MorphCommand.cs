using Core.Database;
using Core.GS.Spells;

namespace Core.GS.Commands;

[Command(
	"&morph", //command to handle
	EPrivLevel.GM, //minimum privelege level
	"Temporarily changes the target player's model", //command description
	"'/morph <modelID> [time]' to change into <modelID> for [time] minutes (default=10)")] //usage
public class MorphCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length == 1)
		{
			DisplaySyntax( client );
			return;
		}

		GamePlayer player = client.Player.TargetObject as GamePlayer;

		if ( player == null )
			player = client.Player;

		ushort model;

		if ( ushort.TryParse( args[1], out model ) == false )
		{
			DisplaySyntax( client );
			return;
		}

		int duration = 10;

		if ( args.Length > 2 )
		{
			if ( int.TryParse( args[2], out duration ) == false )
				duration = 10;
		}

		DbSpell dbSpell = new DbSpell();
		dbSpell.Name = "GM Morph";
		dbSpell.Description = "Target has been shapechanged.";
		dbSpell.ClientEffect = 8000;
		dbSpell.Icon = 805;
		dbSpell.Target = "Realm";
		dbSpell.Range = 4000;
		dbSpell.Power = 0;
		dbSpell.CastTime = 0;
		dbSpell.Type = ESpellType.Morph.ToString();
		dbSpell.Duration = duration * 60;
		dbSpell.LifeDrainReturn = model;

		Spell morphSpell = new Spell( dbSpell, 0 );
		SpellLine gmLine = new SpellLine( "GMSpell", "GM Spell", "none", false );

		ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler( client.Player, morphSpell, gmLine );

		if ( spellHandler == null )
		{
			DisplayMessage( client, "Unable to create spell handler." );
		}
		else
		{
			spellHandler.StartSpell( player );
		}
	}
}