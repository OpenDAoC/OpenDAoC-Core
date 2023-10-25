using Core.GS.Server;

namespace Core.GS;

[NpcGuildScript("Guild Registrar")]
public class GuildRegistrar : GameNpc
{
	protected const string FORM_A_GUILD = "form a guild";

	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;

		SayTo(player, "Hail, " + player.PlayerClass.Name + ". Have you come to [" + FORM_A_GUILD + "]?");

		return true;
	}

	public override bool WhisperReceive(GameLiving source, string text)
	{
		if (!base.WhisperReceive(source, text))
			return false;
		if (source is GamePlayer == false)
			return true;
		GamePlayer player = (GamePlayer)source;

		switch (text)
		{
			case FORM_A_GUILD:
				SayTo(player,
					"Well, then. This can be done. Gather together " + ServerProperty.GUILD_NUM +
					" people who would join with you, and bring them here. The price will be one gold. After I am paid, use /gc form <guildname>. Then I will ask you all if you wish to form such a guild. All must choose to form the guild. It's quite simple, really.");
				break;
		}

		return true;
	}
}